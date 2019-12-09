/* Shared Functions */

//Get current tab by index
function getCurrentTabIndex() {
    var selected = $('#tabs').tabs('option', 'active');
    return selected;
};

//Show errors
function showErrorDialog(errorHTML) {
    $("#errorMsg").append(errorHTML.toString());
    $("#error-dialog").dialog("open");
};

//Show info
function showInfoDialog(infoHTML) {
    $("#infoMsg").append(infoHTML.toString());
    $("#info-dialog").dialog("open");
};

//Object for custom error
function errorObject(error) {
    this.Message = error;
}

//Common function for handling errors
function ErrorHandling(xhr, source) {
    var err;
    $("#errorMsg").empty();

    //Check request was not just cancelled
    //cancel is not an error, error event raised by jQuery when ajax cancelled
    if (xhr.readyState == 0 || xhr.status == 0) {
        return;
    }

    try {
        //Retrieve message JSON object from XHR
        err = $.parseJSON(xhr.responseText);
    }
    catch (e) {
  
    }

    if (xhr.responseText == "" || err == undefined) {     
        err = new errorObject(xhr.status + " - " + xhr.statusText);
    }

    if((xhr.status == 500) || (xhr.status == 12030)){
        //Was it a Session time out?
        if (err.Message == "Session State Timeout") {
            showErrorDialog('Session Time Out');
            setTimeout(function () { window.location.replace("../Default.aspx") }, 2000);
        }
        else {

            showErrorDialog('Internal Server Error.');
        }
    }
    else if ((xhr.status == 502) || (xhr.status == 12031)) {
        showErrorDialog('Server not found.');
    }
    //Everyone else.
    else {
        showErrorDialog('Error loading ' + source + ': <br />' +
                                                   err.Message +
                                                       '<br />');
    }
};