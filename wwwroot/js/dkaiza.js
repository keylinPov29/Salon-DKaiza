document.addEventListener("DOMContentLoaded", function () {

    const inicio = document.getElementById("inicio");
    const servicios = document.getElementById("servicios");

    const navServicios = document.getElementById("navServicios");
    const btnReservar = document.getElementById("btnReservar");
    const btnAtras = document.getElementById("btnAtras");
    const brand = document.getElementById("brand");
    const footerServicios = document.getElementById("footerServicios");

    const enlacesAncla = document.querySelectorAll("[data-ancla]");


    // ==========================================
    // MOSTRAR SERVICIOS
    // ==========================================

    function mostrarServicios() {
        inicio.hidden = true;
        servicios.hidden = false;
        window.scrollTo({ top: 0, behavior: "smooth" });
    }


    // ==========================================
    // VOLVER AL INICIO
    // ==========================================

    function mostrarInicio() {
        servicios.hidden = true;
        inicio.hidden = false;

        // Cierra todas las categorías abiertas
        document.querySelectorAll(".dk-cat__header").forEach(function (header) {
            header.setAttribute("aria-expanded", "false");
            const bodyId = header.getAttribute("aria-controls");
            const body = document.getElementById(bodyId);
            if (body) body.hidden = true;
        });

        window.scrollTo({ top: 0, behavior: "smooth" });
    }


    // ==========================================
    // BOTÓN SERVICIOS (NAVBAR)
    // ==========================================

    if (navServicios) {
        navServicios.addEventListener("click", function () {
            mostrarServicios();
        });
    }


    // ==========================================
    // BOTÓN SERVICIOS (FOOTER)
    // ==========================================

    if (footerServicios) {
        footerServicios.addEventListener("click", function (event) {
            event.preventDefault();
            mostrarServicios();
        });
    }


    // ==========================================
    // BOTÓN RESERVAR MI CITA (HERO)
    // ==========================================

    if (btnReservar) {
        btnReservar.addEventListener("click", function () {
            mostrarServicios();
        });
    }


    // ==========================================
    // BOTÓN ATRÁS
    // ==========================================

    if (btnAtras) {
        btnAtras.addEventListener("click", function () {
            mostrarInicio();
        });
    }


    // ==========================================
    // LOGO (vuelve al inicio)
    // ==========================================

    if (brand) {
        brand.addEventListener("click", function (event) {
            event.preventDefault();
            mostrarInicio();
        });
    }


    // ==========================================
    // ENLACES NOSOTROS / CONTACTO
    // ==========================================

    enlacesAncla.forEach(function (enlace) {
        enlace.addEventListener("click", function (event) {
            event.preventDefault();

            const id = enlace.dataset.ancla;
            const elemento = document.getElementById(id);

            if (!elemento) {
                return;
            }

            servicios.hidden = true;
            inicio.hidden = false;

            setTimeout(function () {
                elemento.scrollIntoView({
                    behavior: "smooth",
                    block: "start"
                });
            }, 50);
        });
    });


    // ==========================================
    // ACORDEÓN DE CATEGORÍAS
    // ==========================================

    const categorias = document.querySelectorAll(".dk-cat__header");

    categorias.forEach(function (categoria) {
        categoria.addEventListener("click", function () {

            const id = categoria.getAttribute("aria-controls");
            const contenido = document.getElementById(id);
            const estaAbierto = categoria.getAttribute("aria-expanded") === "true";

            // Cierra todas las categorías
            categorias.forEach(function (otraCategoria) {
                const otroId = otraCategoria.getAttribute("aria-controls");
                const otroContenido = document.getElementById(otroId);

                otraCategoria.setAttribute("aria-expanded", "false");
                if (otroContenido) otroContenido.hidden = true;
            });

            // Si estaba cerrado, abrirlo
            if (!estaAbierto) {
                categoria.setAttribute("aria-expanded", "true");
                if (contenido) contenido.hidden = false;
            }
        });
    });

});