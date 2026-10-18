using System.Security.Claims;
using DKaiza.Data;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;

    public AccountController(ApplicationDbContext db) => _db = db;

    [HttpGet("/cuenta/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost("/cuenta/login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim().ToLower();
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (usuario == null || !usuario.Activo ||
            !BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash))
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = model.Recordarme });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        if (usuario.Rol == RolUsuario.Administrador)
            return Redirect("/admin/categorias");
            //si es estilista
        if (usuario.Rol == RolUsuario.Estilista)
            return RedirectToAction("Index", "Agenda");

        // Si es cliente, se queda en la página principal
        return Redirect("/");
    }

    [HttpGet("/cuenta/registro")]
    public IActionResult Registro(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/");
        ViewBag.ReturnUrl = returnUrl;
        return View(new RegistroViewModel());
    }

    [HttpPost("/cuenta/registro")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Registro(RegistroViewModel model, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/");

        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var emailNormalizado = model.Email.Trim().ToLower();

        var existeEmail = await _db.Usuarios
            .AnyAsync(u => u.Email.ToLower() == emailNormalizado);

        if (existeEmail)
        {
            ModelState.AddModelError("Email", "Ya existe una cuenta registrada con este correo electrónico.");
            return View(model);
        }

        var usuario = new Usuario
        {
            Nombre = model.Nombre.Trim(),
            Apellido = model.Apellido.Trim(),
            Email = emailNormalizado,
            Telefono = model.Telefono.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Rol = RolUsuario.Cliente,
            Activo = true,
            DebeCambiarPassword = false,
            FechaRegistro = DateTime.UtcNow
        };

        try
        {
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Ocurrió un error al registrar la cuenta. Por favor, intenta de nuevo.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        TempData["Success"] = $"¡Registro exitoso! Te damos la bienvenida a D'Kaiza, {usuario.Nombre}.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return Redirect("/");
    }
        // ───────── Mi perfil (actualización de datos del cliente) ─────────

    private async Task<Usuario?> UsuarioActualAsync()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return null;
        return await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Activo);
    }

    private static PerfilViewModel PerfilDe(Usuario u) => new()
    {
        Nombre = u.Nombre,
        Apellido = u.Apellido,
        Email = u.Email,
        Telefono = u.Telefono,
        FechaRegistro = u.FechaRegistro
    };

    [Authorize(Roles = "Cliente")]
    [HttpGet("/cuenta/perfil")]
    public async Task<IActionResult> Perfil()
    {
        var usuario = await UsuarioActualAsync();
        if (usuario == null) return Redirect("/cuenta/login");
        return View(PerfilDe(usuario));
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost("/cuenta/perfil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilViewModel model)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario == null) return Redirect("/cuenta/login");

        // Nombre, apellido y fecha no se editan: se vuelven a cargar para mostrarlos
        model.Nombre = usuario.Nombre;
        model.Apellido = usuario.Apellido;
        model.FechaRegistro = usuario.FechaRegistro;

        if (!ModelState.IsValid)
        {
            ViewBag.Editando = true;
            return View(model);
        }

        var email = model.Email.Trim().ToLower();

        var duplicado = await _db.Usuarios
            .AnyAsync(u => u.Id != usuario.Id && u.Email.ToLower() == email);

        if (duplicado)
        {
            ModelState.AddModelError("Email", "Ya existe una cuenta registrada con este correo electrónico.");
            ViewBag.Editando = true;
            return View(model);
        }

        usuario.Email = email;
        usuario.Telefono = model.Telefono.Trim();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "No se pudieron guardar los cambios. Intenta de nuevo.");
            ViewBag.Editando = true;
            return View(model);
        }

        // Refrescar la sesión para que el correo nuevo quede en la cookie
        var actual = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            actual?.Properties ?? new AuthenticationProperties());

        TempData["Success"] = "Datos actualizados";
        return Redirect("/cuenta/perfil");
    }

    // La pantalla de citas del cliente aún no existe: por ahora solo avisa.
    // Cuando exista, esta acción pasa a mostrar la lista de citas.
    [Authorize(Roles = "Cliente")]
    [HttpGet("/cuenta/mis-citas")]
    public IActionResult MisCitas()
    {
        TempData["Info"] = "Mis citas estará disponible pronto";
        return Redirect("/cuenta/perfil");
    }
    [HttpPost("/cuenta/logout")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}