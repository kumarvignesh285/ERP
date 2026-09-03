// VMR POWER TOOLS - PREMIUM INTERACTION SCRIPT
$(document).ready(function () {
    // 1. Lenis Smooth Scroll Initiation
    if (typeof Lenis !== 'undefined') {
        const lenis = new Lenis({
            duration: 1.4,
            easing: (t) => Math.min(1, 1.001 - Math.pow(2, -10 * t)),
            orientation: 'vertical',
            gestureOrientation: 'vertical',
            smoothWheel: true,
            wheelMultiplier: 1.0,
            touchMultiplier: 1.5,
            smoothTouch: false,
        });

        function raf(time) {
            lenis.raf(time);
            requestAnimationFrame(raf);
        }
        requestAnimationFrame(raf);

        // Connect GSAP ScrollTrigger to Lenis Scroll
        lenis.on('scroll', () => {
            ScrollTrigger.update();
        });

        gsap.ticker.add((time) => {
            lenis.raf(time * 1000);
        });
        gsap.ticker.lagSmoothing(0);

        // Add support for anchor links using Lenis
        $('a[href^="#"]').on('click', function(e) {
            const target = $(this.getAttribute('href'));
            if(target.length) {
                e.preventDefault();
                lenis.scrollTo(target[0]);
            }
        });
    }

    // 2. Custom Luxury Cursor Tracking
    const cursor = document.getElementById('customCursor');
    const cursorGlow = document.getElementById('customCursorGlow');

    if (cursor && cursorGlow) {
        let mouseX = window.innerWidth / 2;
        let mouseY = window.innerHeight / 2;
        let cursorX = mouseX;
        let cursorY = mouseY;
        let glowX = mouseX;
        let glowY = mouseY;
        let isMoving = false;

        document.addEventListener('mousemove', (e) => {
            mouseX = e.clientX;
            mouseY = e.clientY;
            isMoving = true;
        });

        function animateCursor() {
            // Core cursor (spring index 0.2)
            cursorX += (mouseX - cursorX) * 0.22;
            cursorY += (mouseY - cursorY) * 0.22;
            cursor.style.transform = `translate3d(${cursorX}px, ${cursorY}px, 0) translate(-50%, -50%)`;

            // Lagging glow (spring index 0.1)
            glowX += (mouseX - glowX) * 0.12;
            glowY += (mouseY - glowY) * 0.12;
            cursorGlow.style.transform = `translate3d(${glowX}px, ${glowY}px, 0) translate(-50%, -50%)`;

            requestAnimationFrame(animateCursor);
        }
        animateCursor();

        // Mouse click compression animation
        document.addEventListener('mousedown', () => {
            cursor.style.transform += ' scale(0.6)';
            cursorGlow.style.transform += ' scale(1.4)';
            cursorGlow.style.borderColor = '#ffffff';
        });

        document.addEventListener('mouseup', () => {
            cursor.style.transform = cursor.style.transform.replace(' scale(0.6)', '');
            cursorGlow.style.transform = cursorGlow.style.transform.replace(' scale(1.4)', '');
            cursorGlow.style.borderColor = 'rgba(255, 92, 0, 0.4)';
        });

        // Delegate mouse interactions for hover expansion
        $(document).on('mouseenter', 'a, button, select, input, [role="button"], .interactable', function () {
            cursor.classList.add('custom-cursor-hover');
            cursorGlow.classList.add('custom-cursor-glow-hover');
        });

        $(document).on('mouseleave', 'a, button, select, input, [role="button"], .interactable', function () {
            cursor.classList.remove('custom-cursor-hover');
            cursorGlow.classList.remove('custom-cursor-glow-hover');
        });
    }

    // 3. Magnetic Hover Effect for Premium Elements
    $(document).on('mousemove', '.btn-magnetic', function(e) {
        const bound = this.getBoundingClientRect();
        const x = e.clientX - bound.left - (bound.width / 2);
        const y = e.clientY - bound.top - (bound.height / 2);
        
        gsap.to(this, {
            x: x * 0.35,
            y: y * 0.35,
            duration: 0.3,
            ease: "power2.out"
        });
    });

    $(document).on('mouseleave', '.btn-magnetic', function() {
        gsap.to(this, {
            x: 0,
            y: 0,
            duration: 0.5,
            ease: "elastic.out(1, 0.3)"
        });
    });
});
