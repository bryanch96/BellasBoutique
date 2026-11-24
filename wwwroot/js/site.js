(() => {
    'use strict';

    const body = document.body;
    const reducirMovimiento = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // 1) Animación de aparición al hacer scroll (respetando prefers-reduced-motion).
    const reveals = document.querySelectorAll('.reveal');
    if (reducirMovimiento || !('IntersectionObserver' in window)) {
        reveals.forEach(el => el.classList.add('visible'));
    } else {
        const io = new IntersectionObserver(entries => {
            entries.forEach(e => {
                if (e.isIntersecting) {
                    e.target.classList.add('visible');
                    io.unobserve(e.target);
                }
            });
        }, { threshold: 0.12 });
        reveals.forEach(el => io.observe(el));
    }

    // 2) Cerrar modales de error.
    document.querySelectorAll('[data-close-modal]').forEach(btn =>
        btn.addEventListener('click', () => btn.closest('.modal-overlay')?.remove()));

    // 3) Validación de contraseña en tiempo real (registro).
    const pass = document.querySelector('input[data-pass-check]');
    const listaReglas = document.querySelector('[data-pass-rules]');
    if (pass && listaReglas) {
        const reglas = [
            ['Mínimo 8 caracteres', v => v.length >= 8],
            ['Una mayúscula', v => /[A-Z]/.test(v)],
            ['Una minúscula', v => /[a-z]/.test(v)],
            ['Un número', v => /[0-9]/.test(v)],
            ['Un carácter especial', v => /[^a-zA-Z0-9]/.test(v)],
        ];
        listaReglas.innerHTML = reglas.map(r => `<li data-ok="false">${r[0]}</li>`).join('');
        const items = Array.from(listaReglas.children);
        const actualizar = () => items.forEach((li, i) => li.dataset.ok = String(reglas[i][1](pass.value)));
        pass.addEventListener('input', actualizar);
        actualizar();
    }

    // 4) Campos de tarjeta solo cuando el método es "Tarjeta".
    const camposTarjeta = document.getElementById('tarjeta-campos');
    if (camposTarjeta) {
        const sincronizar = () => {
            const sel = document.querySelector('input[name="Metodo"]:checked');
            const esTarjeta = !sel || sel.value === 'Tarjeta';
            camposTarjeta.hidden = !esTarjeta;
            camposTarjeta.querySelectorAll('input').forEach(i => i.disabled = !esTarjeta);
        };
        document.querySelectorAll('input[name="Metodo"]').forEach(r => r.addEventListener('change', sincronizar));
        sincronizar();
    }

    // 5) Sesión: aviso a los 4 min de inactividad y cierre a los 5 min.
    if (body.dataset.auth === 'true') {
        const LIMITE = 5 * 60 * 1000;
        const AVISO = 4 * 60 * 1000;
        const PING_CADA = 60 * 1000;
        const aviso = document.getElementById('sesion-aviso');
        const segundos = document.getElementById('sesion-segundos');
        const seguir = document.getElementById('sesion-seguir');
        let ultimo = Date.now();
        let ultimoPing = Date.now();
        let avisoVisible = false;

        const ping = () => {
            ultimoPing = Date.now();
            fetch('/Account/Ping', { credentials: 'same-origin' }).catch(() => {});
        };
        const mostrarAviso = () => { avisoVisible = true; aviso.hidden = false; seguir.focus(); };
        const ocultarAviso = () => { avisoVisible = false; aviso.hidden = true; };

        const actividad = () => {
            if (avisoVisible) return; // con el aviso abierto solo cuenta "Seguir conectado"
            ultimo = Date.now();
            if (ultimo - ultimoPing > PING_CADA) ping(); // mantiene viva la cookie del servidor
        };
        ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'].forEach(ev =>
            document.addEventListener(ev, actividad, { passive: true }));

        seguir.addEventListener('click', () => { ultimo = Date.now(); ping(); ocultarAviso(); });

        setInterval(() => {
            const inactivo = Date.now() - ultimo;
            if (inactivo >= LIMITE) {
                const form = document.getElementById('logoutForm');
                if (form) {
                    form.action = '/Account/Logout?timeout=1';
                    form.submit();
                } else {
                    location.href = '/Account/Login?timeout=1';
                }
                return;
            }
            if (inactivo >= AVISO) {
                if (!avisoVisible) mostrarAviso();
                segundos.textContent = String(Math.max(0, Math.ceil((LIMITE - inactivo) / 1000)));
            }
        }, 1000);
    }

    // 6) Chat en tiempo real (SignalR). Un único hub por página.
    const auth = body.dataset.auth === 'true';
    const esPersonal = body.dataset.staff === 'true';
    const miId = Number(body.dataset.userId);
    const badge = document.getElementById('chat-badge');
    const enPaginaChat = document.getElementById('chat-root') !== null;

    if (auth && window.signalR) {
        const conexion = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat')
            .withAutomaticReconnect()
            .build();
        window.bbHub = conexion;

        conexion.on('RecibirMensaje', m => {
            document.dispatchEvent(new CustomEvent('bb:mensaje', { detail: m }));

            // Personal: aviso de mensaje nuevo de un cliente fuera del centro de chat.
            if (esPersonal && !m.esStaff && m.remitenteId !== miId && !enPaginaChat) {
                if (badge) {
                    badge.hidden = false;
                    badge.textContent = String(Number(badge.textContent || 0) + 1);
                }
                const toast = document.createElement('div');
                toast.className = 'toast';
                toast.setAttribute('role', 'status');
                toast.textContent = `Nuevo mensaje de ${m.nombre}`;
                document.body.appendChild(toast);
                setTimeout(() => toast.remove(), 4000);
            }
        });

        conexion.start().catch(err => console.error('Chat no disponible:', err));
    }

    // 7) Página de chat: render de burbujas y envío.
    const root = document.getElementById('chat-root');
    if (root) {
        const modo = root.dataset.modo;
        const clienteActual = Number(root.dataset.clienteId) || null;
        const log = document.getElementById('chat-log');
        const form = document.getElementById('chat-form');
        const input = document.getElementById('chat-input');

        if (badge) { badge.hidden = true; badge.textContent = '0'; }
        if (log) log.scrollTop = log.scrollHeight;

        const agregarBurbuja = (texto, propia, hora) => {
            const div = document.createElement('div');
            div.className = 'bubble ' + (propia ? 'own' : 'other');
            div.textContent = texto;
            if (hora) {
                const s = document.createElement('small');
                s.textContent = ' ' + hora;
                div.appendChild(s);
            }
            log.appendChild(div);
            log.scrollTop = log.scrollHeight;
        };

        document.addEventListener('bb:mensaje', ev => {
            const m = ev.detail;
            if (!log) return;
            const corresponde = modo === 'cliente'
                ? m.clienteId === miId
                : m.clienteId === clienteActual;
            if (!corresponde) return;
            agregarBurbuja(m.texto, m.remitenteId === miId, m.fecha);
        });

        if (form && input) {
            form.addEventListener('submit', async e => {
                e.preventDefault();
                const texto = input.value.trim();
                if (!texto || !window.bbHub) return;
                try {
                    await window.bbHub.invoke('EnviarMensaje', texto, modo === 'personal' ? clienteActual : null);
                    input.value = '';
                } catch (err) {
                    console.error(err);
                }
            });
        }
    }
})();
