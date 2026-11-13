(() => {
    const token = document.querySelector('meta[name="api-token"]')?.content;
    const message = document.getElementById("student-message");
    const body = document.querySelector("#student-grade-table tbody");

    function showMessage(text, kind) {
        message.textContent = text;
        message.className = `alert alert-${kind}`;
    }

    function addCell(row, value) {
        const cell = document.createElement("td");
        cell.textContent = value;
        row.appendChild(cell);
    }

    async function loadGrades() {
        try {
            const response = await fetch("/api/student/grades", {
                headers: { Authorization: `Bearer ${token}`, Accept: "application/json" }
            });
            if (response.status === 401 || response.status === 403) {
                showMessage("You are not authorized to view these grades. Please sign in with your student account.", "warning");
                return;
            }
            if (!response.ok) {
                showMessage("Grades could not be loaded. Please try again later.", "danger");
                return;
            }

            const grades = await response.json();
            if (grades.length === 0) {
                showMessage("No grades are available for your account yet.", "info");
                return;
            }

            body.replaceChildren();
            for (const grade of grades) {
                const row = document.createElement("tr");
                addCell(row, `${grade.subjectCode} — ${grade.subjectName}`);
                addCell(row, String(grade.grade));
                addCell(row, grade.remarks);
                addCell(row, grade.semester);
                addCell(row, grade.schoolYear);
                body.appendChild(row);
            }
            message.classList.add("d-none");
        } catch {
            showMessage("The grade service is unavailable. Check your connection and try again.", "danger");
        }
    }

    loadGrades();
})();
