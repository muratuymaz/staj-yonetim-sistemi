const logoutForm = document.getElementById("logoutForm");

logoutForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    await fetch(logoutForm.action, {
        method: "POST",
        body: new FormData(logoutForm)
    });

    window.location.href = "/Account/Login";
});
