/* =========================================================
   CHATBOT — Lógica de la ventana emergente
   Salón D'Kaiza
   ========================================================= */
(function () {
    "use strict";

    // ── Referencias DOM ────────────────────────────────────
    var fab           = document.getElementById("chatbot-toggle");
    var chatWindow    = document.getElementById("chatbot-window");
    var closeBtn      = document.getElementById("chatbot-close");
    var messagesDiv   = document.getElementById("chatbot-messages");
    var suggestionsDiv= document.getElementById("chatbot-suggestions");
    var inputText     = document.getElementById("chatbot-input-text");
    var sendBtn       = document.getElementById("chatbot-send");
    var charCount     = document.getElementById("chatbot-char-count");
    var iconOpen      = document.getElementById("chatbot-icon-open");
    var iconClose     = document.getElementById("chatbot-icon-close");

    if (!fab || !chatWindow) return;

    var isOpen = false;
    var hasLoaded = false; // Para cargar solo una vez
    var MAX_CHARS = 200;

    // ── Abrir / Cerrar chat ────────────────────────────────
    fab.addEventListener("click", function () {
        if (isOpen) {
            cerrarChat();
        } else {
            abrirChat();
        }
    });

    closeBtn.addEventListener("click", function () {
        cerrarChat();
    });

    function abrirChat() {
        isOpen = true;
        chatWindow.hidden = false;
        fab.classList.add("chatbot-fab--active");
        iconOpen.style.display = "none";
        iconClose.style.display = "block";

        // Forzar reflow para la animación
        void chatWindow.offsetHeight;
        chatWindow.classList.add("chatbot-window--open");

        if (!hasLoaded) {
            hasLoaded = true;
            cargarChatInicial();
        }

        setTimeout(function () {
            inputText.focus();
        }, 350);
    }

    function cerrarChat() {
        isOpen = false;
        chatWindow.classList.remove("chatbot-window--open");
        fab.classList.remove("chatbot-fab--active");
        iconOpen.style.display = "block";
        iconClose.style.display = "none";

        setTimeout(function () {
            chatWindow.hidden = true;
        }, 300);
    }

    // ── Cargar mensaje de bienvenida + sugerencias ─────────
    function cargarChatInicial() {
        agregarMensajeBot(
            "¡Hola! 👋 Soy el asistente virtual de <strong>D'Kaiza</strong>.<br>" +
            "Estoy aquí para ayudarte con tus dudas sobre servicios, precios, horarios y más.<br><br>" +
            "Selecciona una opción rápida o escríbeme tu consulta 💬"
        );

        // Cargar sugerencias de forma asíncrona
        fetch("/api/chatbot/sugerencias")
            .then(function (res) { return res.json(); })
            .then(function (sugerencias) {
                renderizarSugerencias(sugerencias);
            })
            .catch(function () {
                // Fallback si la API falla
                var fallback = [
                    { id: "precios",   texto: "💰 Precios y Servicios" },
                    { id: "horarios",  texto: "🕐 Horarios de atención" },
                    { id: "ubicacion", texto: "📍 Ubicación del salón" },
                    { id: "reserva",   texto: "📅 ¿Cómo reservar una cita?" },
                    { id: "contacto",  texto: "📞 Contacto" }
                ];
                renderizarSugerencias(fallback);
            });
    }

    function renderizarSugerencias(sugerencias) {
        suggestionsDiv.innerHTML = "";
        sugerencias.forEach(function (sug) {
            var btn = document.createElement("button");
            btn.className = "chatbot-sug-btn";
            btn.textContent = sug.texto;
            btn.type = "button";
            btn.addEventListener("click", function () {
                enviarConsulta(sug.texto);
            });
            suggestionsDiv.appendChild(btn);
        });
    }

    // ── Enviar mensaje ─────────────────────────────────────
    sendBtn.addEventListener("click", function () {
        var texto = inputText.value.trim();
        if (texto.length > 0 && texto.length <= MAX_CHARS) {
            enviarConsulta(texto);
        }
    });

    inputText.addEventListener("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            var texto = inputText.value.trim();
            if (texto.length > 0 && texto.length <= MAX_CHARS) {
                enviarConsulta(texto);
            }
        }
    });

    // ── Contador de caracteres ─────────────────────────────
    inputText.addEventListener("input", function () {
        var len = inputText.value.length;
        charCount.textContent = len + "/" + MAX_CHARS;
        sendBtn.disabled = len === 0 || len > MAX_CHARS;

        if (len > MAX_CHARS * 0.9) {
            charCount.style.color = "#B83232";
        } else {
            charCount.style.color = "";
        }
    });

    // ── Consultar API ──────────────────────────────────────
    function enviarConsulta(texto) {
        agregarMensajeUsuario(texto);
        inputText.value = "";
        charCount.textContent = "0/" + MAX_CHARS;
        sendBtn.disabled = true;

        // Indicador de escritura
        var typing = mostrarEscribiendo();

        fetch("/api/chatbot/consultar", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ mensaje: texto })
        })
        .then(function (res) { return res.json(); })
        .then(function (data) {
            quitarEscribiendo(typing);
            agregarMensajeBot(data.respuesta);
        })
        .catch(function () {
            quitarEscribiendo(typing);
            agregarMensajeBot(
                "⚠️ Hubo un problema al procesar tu consulta. Por favor, inténtalo de nuevo."
            );
        });
    }

    // ── Renderizar mensajes ────────────────────────────────
    function agregarMensajeBot(html) {
        var div = document.createElement("div");
        div.className = "chatbot-msg chatbot-msg--bot";
        div.innerHTML = html;
        messagesDiv.appendChild(div);
        scrollAbajo();
    }

    function agregarMensajeUsuario(texto) {
        var div = document.createElement("div");
        div.className = "chatbot-msg chatbot-msg--user";
        div.textContent = texto;
        messagesDiv.appendChild(div);
        scrollAbajo();
    }

    function mostrarEscribiendo() {
        var div = document.createElement("div");
        div.className = "chatbot-msg chatbot-msg--bot chatbot-msg--typing";
        div.innerHTML = "<span></span><span></span><span></span>";
        messagesDiv.appendChild(div);
        scrollAbajo();
        return div;
    }

    function quitarEscribiendo(el) {
        if (el && el.parentNode) {
            el.parentNode.removeChild(el);
        }
    }

    function scrollAbajo() {
        setTimeout(function () {
            messagesDiv.scrollTop = messagesDiv.scrollHeight;
        }, 50);
    }

})();

