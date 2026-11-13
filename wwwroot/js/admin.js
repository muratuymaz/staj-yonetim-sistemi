const datasetFile = document.getElementById("datasetFile");
const datasetFileName = document.getElementById("datasetFileName");
const datasetMessage = document.getElementById("datasetMessage");
const datasetPreviewButton = document.getElementById("datasetPreviewButton");
const datasetImportButton = document.getElementById("datasetImportButton");
const datasetPreview = document.getElementById("datasetPreview");
const datasetPreviewTable = document.getElementById("datasetPreviewTable");
const datasetRowCount = document.getElementById("datasetRowCount");
const referenceNumberSearch = document.getElementById("referenceNumberSearch");
const referenceSearchEmpty = document.getElementById("referenceSearchEmpty");
const referenceCount = document.getElementById("referenceCount");

let csvRows = [];
let datasetReader = null;
let csvValidationError = "";

function clearCsvPreview() {
    datasetPreview.hidden = true;
    datasetPreviewTable.replaceChildren();
    datasetRowCount.textContent = "";
    datasetPreviewButton.textContent = "Önizlemeyi Göster";
}

function resetDatasetSelection() {
    datasetReader?.abort();
    datasetReader = null;
    csvRows = [];
    csvValidationError = "";
    datasetFile.value = "";
    datasetFileName.textContent = "Henüz dosya seçilmedi.";
    datasetMessage.textContent = "";
    datasetMessage.classList.remove("success-message");
    datasetPreviewButton.disabled = true;
    datasetImportButton.disabled = true;
    clearCsvPreview();
}

// Tırnak içindeki ayraçları ve satır sonlarını hücre değeri olarak okuma
function parseCsv(text) {
    const firstLine = text.split(/\r?\n/, 1)[0];
    const delimiter = (firstLine.match(/;/g) ?? []).length > (firstLine.match(/,/g) ?? []).length ? ";" : ",";
    const rows = [];
    let values = [];
    let value = "";
    let insideQuotes = false;

    for (let index = 0; index < text.length; index++) {
        const character = text[index];

        if (character === '"' && insideQuotes && text[index + 1] === '"') {
            value += '"';
            index++;
        } else if (character === '"') {
            insideQuotes = !insideQuotes;
        } else if (character === delimiter && !insideQuotes) {
            values.push(value.trim());
            value = "";
        } else if ((character === "\r" || character === "\n") && !insideQuotes) {
            if (character === "\r" && text[index + 1] === "\n") index++;
            values.push(value.trim());
            if (values.some(cell => cell !== "")) rows.push(values);
            values = [];
            value = "";
        } else {
            value += character;
        }
    }

    values.push(value.trim());
    if (values.some(cell => cell !== "")) rows.push(values);
    return rows;
}

function normalizeCsvHeader(value) {
    return value.replace(/^\uFEFF/, "").toLocaleLowerCase("tr-TR")
        .replace(/ı/g, "i").normalize("NFD").replace(/[\u0300-\u036f]/g, "")
        .replace(/[^a-z0-9]+/g, " ").trim();
}

// Sağlık kodunu dosyanın tamamında, kaydetmeden önce kontrol etme
function validateCsvHealth(rows) {
    if (rows.length < 2) return "CSV dosyasında kayıt bulunamadı.";

    const headerAliases = ["saglik yardimi statusu", "saglik yardimi", "sigorta durumu", "health aid status"];
    const healthIndex = rows[0].findIndex(header => headerAliases.includes(normalizeCsvHeader(header)));
    if (healthIndex < 0) return "CSV başlığında sağlık yardımı statüsü sütunu bulunamadı.";

    for (let index = 1; index < rows.length; index++) {
        const healthValue = rows[index][healthIndex]?.trim() ?? "";
        if (!/^[1-6]$/.test(healthValue)) {
            return `${index + 1}. satır: sağlık yardımı statüsü '${healthValue.slice(0, 40) || "boş"}' geçersiz; 1-6 arasında olmalıdır.`;
        }
    }

    return "";
}

function escapeHtml(value) {
    return value.replace(/[&<>'"]/g, character => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        "'": "&#39;",
        "\"": "&quot;"
    }[character]));
}

function renderCsvPreview() {
    if (csvRows.length < 2) {
        datasetMessage.textContent = "CSV dosyasında gösterilecek kayıt bulunamadı.";
        datasetPreview.hidden = true;
        return;
    }

    const headers = csvRows[0];
    const rows = csvRows.slice(1, 11);
    const headerHtml = headers.map(header => `<th>${escapeHtml(header)}</th>`).join("");
    const rowHtml = rows.map(row => {
        const cells = headers.map((_, index) => `<td>${escapeHtml(row[index] ?? "")}</td>`).join("");
        return `<tr>${cells}</tr>`;
    }).join("");

    datasetPreviewTable.innerHTML = `<thead><tr>${headerHtml}</tr></thead><tbody>${rowHtml}</tbody>`;
    datasetRowCount.textContent = `${csvRows.length - 1} kayıt bulundu, ilk ${rows.length} kayıt gösteriliyor.`;
    datasetPreview.hidden = false;
    datasetPreviewButton.textContent = "Önizlemeyi Kaldır";
    if (!csvValidationError) {
        datasetMessage.textContent = "Önizleme hazırlandı. Test referanslarını kaydetmek için Referans Veriyi Kaydet butonuna basın.";
        datasetMessage.classList.add("success-message");
    }
}

datasetFile?.addEventListener("change", function () {
    datasetReader?.abort();
    datasetReader = null;
    csvRows = [];
    csvValidationError = "";
    clearCsvPreview();
    const file = datasetFile.files[0];

    if (!file || !file.name.toLowerCase().endsWith(".csv")) {
        datasetFileName.textContent = "Geçerli bir CSV dosyası seçilmedi.";
        datasetMessage.textContent = "";
        datasetMessage.classList.remove("success-message");
        datasetPreviewButton.disabled = true;
        datasetImportButton.disabled = true;
        datasetFile.value = "";
        return;
    }

    if (file.size > 10 * 1024 * 1024) {
        datasetFileName.textContent = "CSV dosyası 10 MB'dan büyük olamaz.";
        datasetMessage.textContent = "";
        datasetMessage.classList.remove("success-message");
        datasetPreviewButton.disabled = true;
        datasetImportButton.disabled = true;
        datasetFile.value = "";
        return;
    }

    datasetFileName.textContent = `${file.name} (${Math.ceil(file.size / 1024)} KB)`;
    datasetMessage.textContent = "Dosya seçildi. Önizlemeyi göstermek için butona basın.";
    datasetMessage.classList.remove("success-message");
    datasetPreviewButton.disabled = true;
    datasetImportButton.disabled = true;

    const reader = new FileReader();
    datasetReader = reader;
    reader.onload = event => {
        if (datasetReader !== reader) return;
        csvRows = parseCsv(event.target.result);
        csvValidationError = validateCsvHealth(csvRows);
        datasetPreviewButton.disabled = false;
        datasetImportButton.disabled = csvValidationError !== "";
        datasetMessage.textContent = csvValidationError
            ? `${file.name}: ${csvValidationError}`
            : "Dosya okundu. Önizlemeyi gösterebilir veya referans veriyi kaydedebilirsiniz.";
    };
    reader.onerror = () => {
        if (datasetReader !== reader) return;
        datasetMessage.textContent = "CSV dosyası okunamadı.";
        datasetPreviewButton.disabled = true;
        datasetImportButton.disabled = true;
    };
    reader.readAsText(file, "UTF-8");
});

datasetPreviewButton?.addEventListener("click", function () {
    if (datasetPreview.hidden) {
        renderCsvPreview();
    } else {
        resetDatasetSelection();
    }
});

// Öğrenci numarasına göre kaydedilen referansları süzme
referenceNumberSearch?.addEventListener("input", function () {
    const studentNumber = referenceNumberSearch.value.trim();
    const rows = document.querySelectorAll(".reference-table tbody tr[data-student-number]");
    let visibleCount = 0;

    rows.forEach(row => {
        row.hidden = !row.dataset.studentNumber.includes(studentNumber);
        if (!row.hidden) visibleCount++;
    });

    referenceSearchEmpty.hidden = visibleCount > 0;
    referenceCount.textContent = studentNumber
        ? `${visibleCount} / ${rows.length} kayıt`
        : `${rows.length} kayıt`;
});

const registeredNumberSearch = document.getElementById("registeredNumberSearch");
const registeredSearchEmpty = document.getElementById("registeredSearchEmpty");
const registeredCount = document.getElementById("registeredCount");
const detailPanel = document.getElementById("adminStudentDetails");
const detailMessage = document.getElementById("detailMessage");
const adminStudentForm = document.getElementById("adminStudentForm");
const completeAdminDocument = document.getElementById("completeAdminDocument");
const deleteAdminDocument = document.getElementById("deleteAdminDocument");
let selectedDocumentId = null;

registeredNumberSearch?.addEventListener("input", function () {
    const number = registeredNumberSearch.value.trim();
    const rows = document.querySelectorAll(".registered-student-row");
    let visibleCount = 0;

    rows.forEach(row => {
        row.hidden = !row.dataset.studentNumber.includes(number);
        if (!row.hidden) visibleCount++;
    });

    registeredSearchEmpty.hidden = visibleCount > 0;
    registeredCount.textContent = number
        ? `${visibleCount} / ${rows.length} öğrenci`
        : `${rows.length} öğrenci`;
});

const fieldLabels = {
    FullName: "Ad soyad", IdentityNumber: "Kimlik no", Nationality: "Uyruk",
    BirthDate: "Doğum tarihi", StudentNumber: "Öğrenci no", Address: "Adres",
    Department: "Bölüm", AcademicYear: "Ek-16 teslim yılı / yarıyılı",
    PreviousInternshipDays: "Önceki staj günleri", Email: "E-posta",
    PhoneNumber: "Telefon", HealthAidStatus: "Sağlık yardımı statüsü",
    InternshipStartDate: "Staj başlangıcı", InternshipEndDate: "Staj bitişi"
};

function renderComparisonRows(targetId, comparisons, unreadableText, emptyText) {
    const target = document.getElementById(targetId);
    target.replaceChildren();

    if (!comparisons) {
        const row = document.createElement("tr");
        const cell = document.createElement("td");
        cell.colSpan = 4;
        cell.textContent = emptyText;
        row.append(cell);
        target.append(row);
        return;
    }

    for (const item of comparisons ?? []) {
        const row = document.createElement("tr");
        const status = item.status === "match" ? "Eşleşiyor"
            : item.status === "mismatch" ? "Uyuşmuyor"
            : item.status === "formValueMissing" ? "Referans değeri boş"
            : unreadableText;

        for (const value of [fieldLabels[item.fieldName] ?? item.fieldName,
            item.documentValue || "—", item.formValue || "—", status]) {
            const cell = document.createElement("td");
            cell.textContent = value;
            row.append(cell);
        }
        target.append(row);
    }
}

function fillAdminForm(profile) {
    const fields = {
        FullName: profile.fullName, StudentNumber: profile.studentNumber,
        IdentityNumber: profile.identityNumber, Nationality: profile.nationality,
        BirthDate: profile.birthDate?.substring(0, 10), Address: profile.address,
        Department: profile.department, AcademicYear: profile.academicYear,
        PreviousInternshipDays: profile.previousInternshipDays, Email: profile.email,
        PhoneNumber: profile.phoneNumber, HealthAidStatus: profile.healthAidStatus,
        InternshipStartDate: profile.internshipStartDate?.startsWith("0001")
            ? "" : profile.internshipStartDate?.substring(0, 10),
        InternshipEndDate: profile.internshipEndDate?.startsWith("0001")
            ? "" : profile.internshipEndDate?.substring(0, 10)
    };

    for (const [name, value] of Object.entries(fields)) {
        adminStudentForm.elements.namedItem(name).value = value ?? "";
    }

    adminStudentForm.querySelectorAll("input[name='InternshipDays']").forEach(input => {
        input.checked = (profile.internshipDays ?? []).includes(input.value);
    });
}

async function showStudentDetails(studentId) {
    detailPanel.hidden = false;
    detailMessage.textContent = "Öğrenci bilgileri yükleniyor...";

    try {
        const response = await fetch(`/Admin/StudentDetails?studentId=${studentId}`);
        if (!response.ok) throw new Error("Öğrenci bilgileri alınamadı.");
        const data = await response.json();
        document.getElementById("adminStudentId").value = data.studentId;
        document.getElementById("detailStudentName").textContent =
            data.profile.fullName || data.userName;
        document.getElementById("detailDocumentStatus").textContent = data.documentName
            ? `${data.documentName} — ${data.documentStatus}`
            : "Ek-16 belgesi yüklenmedi.";
        selectedDocumentId = data.documentId;
        deleteAdminDocument.disabled = !data.documentId;
        fillAdminForm(data.profile);
        adminStudentForm.elements.namedItem("InternshipStartDate").required = !!data.documentId;
        adminStudentForm.elements.namedItem("InternshipEndDate").required = !!data.documentId;

        renderComparisonRows("referenceComparisonRows", data.referenceComparisons,
            "Kayıtlı bilgi yok", "CSV referansı bulunamadı.");
        renderComparisonRows("documentComparisonRows", data.documentComparisons,
            "Belgeden okunamadı", data.documentName
                ? "Belgedeki alanlar okunamadı." : "Ek-16 belgesi yüklenmedi.");

        completeAdminDocument.disabled = !data.canComplete;
        detailMessage.textContent = "";
        detailPanel.scrollIntoView({ block: "start" });
    } catch (error) {
        detailMessage.textContent = error.message;
    }
}

document.querySelectorAll(".registered-student-row").forEach(row => {
    const open = () => {
        const studentId = row.dataset.studentId;
        history.replaceState(null, "", `/Admin?studentId=${studentId}`);
        showStudentDetails(studentId);
    };
    row.addEventListener("click", open);
    row.addEventListener("keydown", event => {
        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            open();
        }
    });
});

document.getElementById("closeStudentDetails")?.addEventListener("click", () => {
    detailPanel.hidden = true;
    history.replaceState(null, "", "/Admin");
});

const initialStudentId = new URLSearchParams(location.search).get("studentId");
if (initialStudentId && /^\d+$/.test(initialStudentId)) {
    showStudentDetails(initialStudentId);
}

async function postAdminAction(url, formData) {
    const response = await fetch(url, { method: "POST", body: formData });
    const text = await response.text();
    if (!response.ok) throw new Error(text || "İşlem tamamlanamadı.");
    return JSON.parse(text);
}

adminStudentForm?.addEventListener("submit", async event => {
    event.preventDefault();
    const button = document.getElementById("saveAdminStudent");
    button.disabled = true;
    try {
        await postAdminAction(adminStudentForm.action, new FormData(adminStudentForm));
        location.reload();
    } catch (error) {
        detailMessage.textContent = error.message;
        button.disabled = false;
    }
});

completeAdminDocument?.addEventListener("click", async () => {
    if (!selectedDocumentId) return;
    completeAdminDocument.disabled = true;
    const data = new FormData();
    data.append("documentId", selectedDocumentId);
    data.append("__RequestVerificationToken",
        adminStudentForm.querySelector("input[name='__RequestVerificationToken']").value);
    try {
        await postAdminAction("/Admin/CompleteDocument", data);
        location.reload();
    } catch (error) {
        detailMessage.textContent = error.message;
        completeAdminDocument.disabled = false;
    }
});

deleteAdminDocument?.addEventListener("click", async () => {
    if (!selectedDocumentId || !confirm("Seçili Ek-16 belgesi silinsin mi?")) return;
    const data = new FormData();
    data.append("documentId", selectedDocumentId);
    data.append("__RequestVerificationToken",
        adminStudentForm.querySelector("input[name='__RequestVerificationToken']").value);
    deleteAdminDocument.disabled = true;
    try {
        await postAdminAction("/Admin/DeleteDocument", data);
        location.reload();
    } catch (error) {
        detailMessage.textContent = error.message;
        deleteAdminDocument.disabled = false;
    }
});
