(() => {
    const token = document.querySelector('meta[name="api-token"]')?.content;
    const message = document.getElementById("faculty-message");
    const tableBody = document.querySelector("#grade-table tbody");
    const studentSelect = document.getElementById("student-id");
    const form = document.getElementById("grade-form");
    const subjectForm = document.getElementById("subject-form");
    const editId = document.getElementById("grade-id");
    const formTitle = document.getElementById("grade-form-title");
    const cancelEdit = document.getElementById("cancel-edit");
    const input = id => document.getElementById(id);
    let grades = [];
    let subjectNames = new Map();

    function showMessage(text, kind = "info") {
        message.textContent = text;
        message.className = `alert alert-${kind}`;
    }

    async function request(url, options = {}) {
        const response = await fetch(url, {
            ...options,
            headers: {
                Accept: "application/json",
                Authorization: `Bearer ${token}`,
                ...(options.body ? { "Content-Type": "application/json" } : {}),
                ...options.headers
            }
        });
        if (!response.ok) {
            let detail = `Request failed (${response.status}).`;
            try {
                const problem = await response.json();
                detail = problem.detail || problem.title || detail;
                if (problem.errors) {
                    detail = Object.values(problem.errors).flat().join(" ");
                }
            } catch {
                // Keep the useful status message when the server does not return JSON.
            }
            throw new Error(detail);
        }
        return response.status === 204 ? null : response.json();
    }

    function cell(row, text) {
        const element = document.createElement("td");
        element.textContent = text;
        row.appendChild(element);
    }

    function populateDatalist(id, values) {
        const datalist = input(id);
        datalist.replaceChildren();
        for (const value of [...new Set(values)].sort()) {
            const option = document.createElement("option");
            option.value = value;
            datalist.appendChild(option);
        }
    }

    function renderSubjects(subjects) {
        const subjectCodeSelect = input("subject-code");
        subjectCodeSelect.replaceChildren(new Option("Select a subject code", ""));
        subjectNames = new Map();
        for (const subject of subjects) {
            subjectNames.set(subject.subjectCode, subject.subjectName);
            subjectCodeSelect.add(new Option(
                `${subject.subjectCode} · ${subject.subjectName}`,
                subject.subjectCode));
        }
    }

    function renderGrades() {
        tableBody.replaceChildren();
        if (grades.length === 0) {
            showMessage("No grade records yet. Use the form below to add one.", "info");
            return;
        }
        message.classList.add("d-none");
        for (const grade of grades) {
            const row = document.createElement("tr");
            cell(row, `${grade.studentNumber} · ${grade.studentName}`);
            cell(row, `${grade.subjectCode} · ${grade.subjectName}`);
            cell(row, String(grade.grade));
            cell(row, grade.remarks);
            cell(row, grade.semester);
            cell(row, grade.schoolYear);

            const actions = document.createElement("td");
            actions.className = "text-nowrap";
            const edit = document.createElement("button");
            edit.className = "btn btn-sm btn-outline-primary me-2";
            edit.type = "button";
            edit.textContent = "Edit";
            edit.addEventListener("click", () => beginEdit(grade));
            const remove = document.createElement("button");
            remove.className = "btn btn-sm btn-outline-danger";
            remove.type = "button";
            remove.textContent = "Delete";
            remove.addEventListener("click", () => deleteGrade(grade));
            actions.append(edit, remove);
            row.appendChild(actions);
            tableBody.appendChild(row);
        }
    }

    async function load() {
        try {
            const [studentOptions, subjects, records] = await Promise.all([
                request("/api/grades/students"),
                request("/api/grades/subjects"),
                request("/api/grades")
            ]);
            studentSelect.replaceChildren();
            for (const student of studentOptions) {
                const option = document.createElement("option");
                option.value = student.studentId;
                option.textContent = `${student.studentNumber} · ${student.studentName}`;
                studentSelect.appendChild(option);
            }
            renderSubjects(subjects);
            grades = records;
            populateDatalist("semester-options", records.map(grade => grade.semester));
            populateDatalist("school-year-options", records.map(grade => grade.schoolYear));
            renderGrades();
        } catch (error) {
            showMessage(error.message || "Grade records could not be loaded.", "danger");
        }
    }

    input("subject-code").addEventListener("change", () => {
        input("subject-name").value = subjectNames.get(input("subject-code").value) || "";
    });

    function beginEdit(grade) {
        editId.value = grade.gradeId;
        studentSelect.value = grade.studentId;
        input("subject-code").value = grade.subjectCode;
        input("subject-name").value = grade.subjectName;
        input("grade-value").value = grade.grade;
        input("semester").value = grade.semester;
        input("school-year").value = grade.schoolYear;
        formTitle.textContent = "Edit grade";
        cancelEdit.classList.remove("d-none");
        form.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function resetForm() {
        form.reset();
        editId.value = "";
        formTitle.textContent = "Add a grade";
        cancelEdit.classList.add("d-none");
    }

    async function deleteGrade(grade) {
        if (!window.confirm(`Delete ${grade.subjectCode} for ${grade.studentName}? This cannot be undone.`)) {
            return;
        }
        try {
            await request(`/api/grades/${grade.gradeId}`, { method: "DELETE" });
            showMessage("Grade deleted.", "success");
            await load();
        } catch (error) {
            showMessage(error.message, "danger");
        }
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        const gradeId = editId.value;
        const payload = {
            studentId: Number(studentSelect.value),
            subjectCode: input("subject-code").value.trim(),
            subjectName: input("subject-name").value.trim(),
            grade: Number(input("grade-value").value),
            semester: input("semester").value.trim(),
            schoolYear: input("school-year").value.trim(),
            remarks: "Client value is ignored; the API computes this."
        };
        try {
            const saved = await request(
                gradeId ? `/api/grades/${gradeId}` : "/api/grades",
                { method: gradeId ? "PUT" : "POST", body: JSON.stringify(payload) });
            showMessage(`Grade saved. Server-calculated remark: ${saved.remarks}.`, "success");
            resetForm();
            await load();
        } catch (error) {
            showMessage(error.message, "danger");
        }
    });

    cancelEdit.addEventListener("click", resetForm);

    subjectForm.addEventListener("submit", async event => {
        event.preventDefault();
        const payload = {
            subjectCode: input("new-subject-code").value.trim(),
            subjectName: input("new-subject-name").value.trim()
        };
        try {
            const subject = await request("/api/grades/subjects", {
                method: "POST",
                body: JSON.stringify(payload)
            });
            const subjects = await request("/api/grades/subjects");
            renderSubjects(subjects);
            input("subject-code").value = subject.subjectCode;
            input("subject-name").value = subject.subjectName;
            subjectForm.reset();
            showMessage(`Subject ${subject.subjectCode} added.`, "success");
        } catch (error) {
            showMessage(error.message || "Subject could not be added.", "danger");
        }
    });

    load();
})();
