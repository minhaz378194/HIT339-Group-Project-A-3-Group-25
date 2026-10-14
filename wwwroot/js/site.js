// LibraTrack - Interactive Client Logic & Sticky Glassdoor Navigation
document.addEventListener('DOMContentLoaded', function () {
    const navbar = document.getElementById('mainNavbar') || document.querySelector('.navbar');
    if (!navbar) return;

    let ticking = false;

    function handleNavbarScroll() {
        const scrollDistance = window.pageYOffset || document.documentElement.scrollTop;
        if (scrollDistance > 20) {
            navbar.classList.add('navbar-scrolled');
        } else {
            navbar.classList.remove('navbar-scrolled');
        }
        ticking = false;
    }

    // Attach passive scroll listener for 60fps/120fps fluid performance
    window.addEventListener('scroll', function () {
        if (!ticking) {
            window.requestAnimationFrame(handleNavbarScroll);
            ticking = true;
        }
    }, { passive: true });

    // Initial check on page load
    handleNavbarScroll();
});
