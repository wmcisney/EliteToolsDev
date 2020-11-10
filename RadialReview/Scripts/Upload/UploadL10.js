
var uploader = new UploadSteps({
    defaultData: { recurrenceId: window.recurrenceId, type: "L10", csv: false, noTitleBar: window.noTitleBar },
    uploadFileUrl: "/Upload/UploadL10",
    uploadMultiple: true,

    uploadSelectionUrl: "/Upload/ProcessTodosSelection",
    confirmSelectionUrl: "/Upload/SubmitTodos",
    afterUpload: function (d) {
        uploader.clearSelectionSteps();
        //uploader.addSelectionStep("Select to-dos (Do not select header)", validateTodo);
        //if (d.Data.FileType == "CSV") {
        //    uploader.addSelectionStep("Select due date (Do not select header)", validateDate, true);
        //    uploader.addSelectionStep("Select owners (Do not select header)", validateUsers, true);
        //    uploader.addSelectionStep("Select details (Do not select header)", validateDetails, true);
        //}
    },
    submissionCallback: function () {
        window.location.href = "/L10/wizard/" + window.recurrenceId;
    }
});

function validateTodo(rect) {
    var allTrue = true;
    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);

    issuesRect = rect;
    uploader.addSelectionData("todos", issuesRect);
    return allTrue;
}

function validateUsers(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    userRect = rect;
    uploader.addSelectionData("users", userRect);
    return allTrue;
}

function validateDetails(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    detailsRect = rect;
    uploader.addSelectionData("details", detailsRect);
    return allTrue;
}
function validateDate(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    duedateRect = rect;
    uploader.addSelectionData("duedate", duedateRect);
    return allTrue;
}