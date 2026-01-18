(function () {
        // No changes required unless you want to add client-side validation or UX improvements for the qty input fields.
        // If you want to auto-limit the qty input to available rooms dynamically, you can add JS here.
    let currentAction = null; // "cart" | "reserve" | "buy"
    let currentPackageId = null;
    let lastOk = false;

    const modalEl = document.getElementById("roomsModal");
    if (!modalEl) return;

    const modal = new bootstrap.Modal(modalEl);
    const qtyInput = document.getElementById("thRoomsQty");
    const msgEl = document.getElementById("thRoomsMsg");
    const continueBtn = document.getElementById("thRoomsContinue");

    function setMsg(text, ok) {
        msgEl.textContent = text;
        msgEl.className = ok ? "small mt-2 text-success" : "small mt-2 text-danger";
    }

    async function checkAvailability() {
        lastOk = false;
        continueBtn.disabled = true;

        const qty = Math.max(1, parseInt(qtyInput.value || "1", 10));

        const url = `/TravelPackages/CheckRooms?packageId=${currentPackageId}&qty=${qty}`;
        const res = await fetch(url);
        const data = await res.json();

        setMsg(data.message || "Unknown", data.ok);
        lastOk = !!data.ok;
        continueBtn.disabled = !lastOk;
    }

    document.addEventListener("click", function (e) {
        const btn = e.target.closest("[data-rooms-action]");
        if (!btn) return;

        e.preventDefault();

        currentAction = btn.dataset.roomsAction;
        currentPackageId = btn.dataset.packageId;

        qtyInput.value = "1";
        msgEl.className = "small mt-2 text-muted";
        msgEl.textContent = "Checking availability...";
        continueBtn.disabled = true;
        lastOk = false;

        modal.show();
        checkAvailability();

    });


    qtyInput.addEventListener("input", function () {
        msgEl.className = "small mt-2 text-muted";
        msgEl.textContent = "Checking availability...";
        checkAvailability();
    });


    continueBtn.addEventListener("click", async function () {
        // ✅ תמיד עושים בדיקה עדכנית לכמות לפני שליחה
        await checkAvailability();
        if (!lastOk) return;

        const isAuth = (window.TH_IS_AUTH === true || window.TH_IS_AUTH === "true");
        if (!isAuth) {
            const returnUrl = encodeURIComponent(window.TH_RETURN_URL || "/");
            window.location.href = `/Identity/Account/Login?returnUrl=${returnUrl}`;
            return;
        }

        const qty = Math.max(1, parseInt(qtyInput.value || "1", 10));

        if (currentAction === "cart") {
            document.getElementById("thCartPackageId").value = currentPackageId;
            document.getElementById("thCartQty").value = qty;
            document.getElementById("thFormCart").submit();
        }
        else if (currentAction === "reserve") {
            document.getElementById("thReservePackageId").value = currentPackageId;
            document.getElementById("thReserveQty").value = qty;
            document.getElementById("thFormReserve").submit();
        }
        else if (currentAction === "buy") {
            document.getElementById("thBuyPackageId").value = currentPackageId;
            document.getElementById("thBuyQty").value = qty;
            document.getElementById("thFormBuy").submit();
        }
    });

})();
