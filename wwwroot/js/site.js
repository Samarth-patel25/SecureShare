// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Copy a share link and give visual feedback on the button.
function copyLink(link, btn) {
    var done = function () {
        if (!btn) return;
        var original = btn.innerHTML;
        btn.textContent = "Copied!";
        btn.classList.add("copied");
        setTimeout(function () {
            btn.innerHTML = original;
            btn.classList.remove("copied");
        }, 1500);
    };

    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(link).then(done);
    } else {
        window.prompt("Copy this share link:", link);
    }
}