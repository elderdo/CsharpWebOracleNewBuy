<%@ Page Title="UK CCB" Language="C#" MasterPageFile="~/NewBuy.Master" AutoEventWireup="true" CodeBehind="CCB.aspx.cs" Inherits="NewBuy.UK.CCB" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div id="tabs">
    <ul>
        <li><a href="#tabs-CCB">CCB</a></li>
        <li><a href="#tabs-Part-LTA">Part LTA</a></li>
        <li><a href="#tabs-Summary">Summary</a></li>
    </ul>
    <div id="tabs-CCB">
        <div class="toolbar">
            <button id="ccbButton1" title="Asset Manager" value="">Asset Manager</button>
            <button id="ccbButton2" title="Part Number" value="">Part Number</button>
            <button id="ccbButton3" title="Activity Status" value="">Activity Status</button>
            <button id="ccbButton4" title="View All" value="">View All</button>
        </div>
        <div id="ccbGridWrapper" class="ui-jqgrid gridWrapper">
            <table id="listCCB"></table>
            <div id="pagerCCB"></div>
        </div>
    </div>
    <div id="tabs-Part-LTA">
        <div class="ltaReport">
            <div class="ltaPartDetail">
                <div class="toolbar">
                    <button id="ltaButton2" title="Part Lookup" value="">Find</button>
                </div>
                Part:<span id="ltaDtl_part"></span><br /><br />
                Description: <span id="ltaDtl_description"></span><br /><br />
                PLO: <span id="ltaDtl_plo"></span><br />
                PR: <span id="ltaDtl_pr"></span><br />
                Forecast (<%= DateTime.Today.Year + 1 %>): <span id="ltaDtl_forecast"></span>
            </div>
            <div class="ltaDetail">
                <div class="ltaReportNotes">
                    Last Updated: <asp:Label ID="LTALastUpdate" runat="server" Text=""></asp:Label>
                </div>
                <div id="ltaGridWrapper" class="ui-jqgrid gridWrapper">
                    <table id="listLta"></table>
                    <div id="pagerLta"></div>
                </div>
            </div>
        </div>
    </div>
    <div id="tabs-Summary">
        <div class="toolbar">
            Start:<input id="sumStartDate" type="text" title="Start Date" value="<%= NewBuy.DbInterface.CurrentAcctPeriod.Start %>" /> 
            End:<input id="sumEndDate" type="text" title="End Date" value="<%= NewBuy.DbInterface.CurrentAcctPeriod.End %>" /> 
            <button id="summaryButton1" title="Update Summary" value="">Update</button>
        </div>
        <div id="summaryGridWrapper" class="ui-jqgrid gridWrapper">
            <table id="listSummary"></table>
            <div id="pagerSummary"></div>
        </div>
    </div>
</div>
<div id="lookup-manager" title="Select an Asset Manager">
    <div id="managerGridWrapper" class="ui-jqgrid gridWrapper">
        <table id="listManager"></table>
        <div id="pagerManager"></div>
    </div>
</div>
<div id="lookup-part" title="Select a Part">
    <div class="partSearchDiv">
        <span class="ui-icon ui-icon-search partSearchIcon"></span>
        <input id="partSrcText" class="ui-widget" />
        <div class="inputNote">Input: Letters, Numbers, - / ()</div>
    </div>
    <div id="partGridWrapper" class="ui-jqgrid gridWrapper">
        <table id="listPart"></table>
        <div id="pagerPart"></div>
    </div>
</div>
<div id="lookup-activity" title="Select an Activity Status">
    <div id="activityGridWrapper" class="ui-jqgrid gridWrapper">
        <table id="listActivity"></table>
        <div id="pagerActivity"></div>
    </div>
</div>
<div id="lookup-comments" title="Order Comments">
    <div id="amRemarkGridWrapper" class="ui-jqgrid gridWrapper">
        <table id="listAmRemark"></table>
        <div id="pagerAmRemark"></div>
    </div><br />
    <div id="ccbRemarkGridWrapper" class="ui-jqgrid gridWrapper">
        <div>Not Allowed: <, >, ', "</div>
        <table id="listCcbRemark"></table>
        <div id="pagerCcbRemark"></div>
    </div><br />
    <div id="spoRemarkGridWrapper" class="ui-jqgrid gridWrapper">
        <table id="listSpoRemark"></table>
        <div id="pagerSpoRemark"></div>
    </div>
</div>
<div id="error-dialog" title="Error">
    <p><span class="ui-icon ui-icon-alert dialogIcon"></span><span id="errorMsg"></span></p>
</div>
<div id="info-dialog" title="Info">
    <p><span class="ui-icon ui-icon-info dialogIcon"></span><span id="infoMsg"></span></p>
</div>
<div id="wait-dialog" title="Processing...">
    <p><asp:Image ID="Processing" AlternateText="Processing, please wait..." ImageUrl="~/Content/images/progress_cir.gif" runat="server" /></p>
</div>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContentjQuery" runat="server">
    <script type="text/javascript">
        /* UI Elements */
        $(function () {
            //Tabs
            $("#tabs").tabs
            ({
                activate: function (event, ui) {
                    resizeGrids();
                }
            });

            //Buttons
            //Asset Manager lookup
            $("#ccbButton1").button({
                icons: {
                    primary: "ui-icon-person"
                },
                text: true
            })
            .click(function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                
                //Open Dialog
                $("#lookup-manager").dialog("open");

                //Update grid size
                $("#listManager").setGridWidth($("#managerGridWrapper").width());
                $("#listManager").setGridHeight(300);

                //Prevent default action after close, no postback
                return false;
            });

            //Part lookup
            $("#ccbButton2").button({
                icons: {
                    primary: "ui-icon-gear"
                },
                text: true
            })
            .click(function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                
                //Open Dialog
                $("#lookup-part").dialog("open");

                //Update grid size
                $("#listPart").setGridWidth($("#partGridWrapper").width());
                $("#listPart").setGridHeight(300);

                //Prevent default action after close, no postback
                return false;
            });

            //Activity Status Lookup
            $("#ccbButton3").button({
                icons: {
                    primary: "ui-icon-bookmark"
                },
                text: true
            })
            .click(function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                
                //Open Dialog
                $("#lookup-activity").dialog("open");

                //Update grid size
                $("#listActivity").setGridWidth($("#activityGridWrapper").width());
                $("#listActivity").setGridHeight(300);

                //Prevent default action after close, no postback
                return false;
            });

            //My Export
            $("#ccbButton4").button({
                icons: {
                    primary: "ui-icon-clipboard"
                },
                text: true
            })
            .click(function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                
                //Clear all other buttons
                jQuery('[id ^= ccbButton]').prop('value', '');

                //Reload grid
                jQuery("#listCCB").trigger("reloadGrid");

                //Prevent default action after close, no postback
                return false;
            });

            //Part lookup (lta tab)
            $("#ltaButton2").button({
                icons: {
                    primary: "ui-icon-search"
                },
                text: true,
                disabled: true
            });
            $("#ltaButton2").css('display', 'none');

            //CCB Summary
            $("#summaryButton1").button({
                icons: {
                    primary: "ui-icon-refresh"
                },
                text: true
            })
            .click(function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                
                //Reload grid
                jQuery("#listSummary").trigger("reloadGrid");

                //Prevent default action after close, no postback
                return false;
            });

            //Dates
            $("#sumStartDate").datepicker({
                dateFormat: "mm/dd/yy"
            });

            $("#sumEndDate").datepicker({
                dateFormat: "mm/dd/yy"
            });

            //Dialogs
            $("#lookup-manager").dialog({
                autoOpen: false,
                dialogClass: 'lookup',
                modal: true,
                resizable: false,
                open: function () {
                    //reload grid
                    jQuery("#listManager").trigger("reloadGrid");
                },
                buttons: {
                    "Select": function () {
                        //Get selected row from asset manager lookup
                        var mangrid_sel_id = $('#listManager').jqGrid('getGridParam', 'selrow');
                        if (mangrid_sel_id) {
                            //Clear all other buttons
                            jQuery('[id ^= ccbButton]').prop('value', '');

                            //Set button value
                            jQuery('#ccbButton1').prop('value', mangrid_sel_id);

                            //Reload grid
                            jQuery("#listCCB").trigger("reloadGrid");

                            //Close
                            $(this).dialog("close");
                        }
                    },
                    "Cancel": function () {
                        $(this).dialog("close");
                    }
                }
            });

            $("#lookup-part").dialog({
                autoOpen: false,
                dialogClass: 'lookup',
                modal: true,
                resizable: false,
                open: function () {
                    //Clear input value
                    $("#partSrcText").prop('value', '');

                    //clear buttons
                    if (flagLtaPartGrid) $('#ltaButton2').prop('value', '');
                    if (!flagLtaPartGrid) $('#ccbButton2').prop('value', '');

                    //update grid size
                    $("#listPart").setGridWidth($("#partGridWrapper").width());
                    $("#listPart").setGridHeight(300);

                    //reload grid
                    jQuery("#listPart").trigger("reloadGrid");

                    //reload grid as user types
                    $("#partSrcText").keyup(function () {
                        //set button
                        if (flagLtaPartGrid) $('#ltaButton2').prop('value', $("#partSrcText").prop('value'));
                        if (!flagLtaPartGrid) $('#ccbButton2').prop('value', $("#partSrcText").prop('value'));

                        //reload grid
                        jQuery("#listPart").trigger("reloadGrid");
                    });
                },
                buttons: {
                    "Select": function () {
                        //Get selected row from part lookup
                        var prtgrid_sel_id = $('#listPart').jqGrid('getGridParam', 'selrow');

                        //Select a grid to updated based on flag
                        if (flagLtaPartGrid) {
                            if (prtgrid_sel_id) {
                                //set button value
                                $("#ltaButton2").prop('value', prtgrid_sel_id);

                                //reload grid
                                jQuery("#listLta").trigger("reloadGrid");

                                //reload detail
                                reloadLtaPartDetail();
                            }
                        }
                        else {
                            if (prtgrid_sel_id) {
                                //Clear all other buttons
                                jQuery('[id ^= ccbButton]').prop('value', '');

                                //Set button value
                                jQuery('#ccbButton2').prop('value', prtgrid_sel_id);

                                //Reload grid
                                jQuery("#listCCB").trigger("reloadGrid");
                            }
                        }

                        //Reset flag
                        flagLtaPartGrid = false;

                        //Close
                        $(this).dialog("close");
                    },
                    "Cancel": function () {
                        //Reset flag
                        flagLtaPartGrid = false;

                        //Close
                        $(this).dialog("close");
                    }
                }
            });

            $("#lookup-activity").dialog({
                autoOpen: false,
                dialogClass: 'lookup',
                modal: true,
                resizable: false,
                buttons: {
                    "Select": function () {
                        //Get selected row from status lookup
                        var pgmgrid_sel_id = $('#listActivity').jqGrid('getGridParam', 'selrow');
                        if (pgmgrid_sel_id) {
                            //Clear all other buttons
                            jQuery('[id ^= ccbButton]').prop('value', '');

                            //Set button value
                            jQuery('#ccbButton3').prop('value', pgmgrid_sel_id);

                            //Reload grid
                            jQuery("#listCCB").trigger("reloadGrid");

                            //Close
                            $(this).dialog("close");
                        }
                    },
                    "Cancel": function () {
                        $(this).dialog("close");
                    }
                }
            });

            $("#lookup-comments").dialog({
                autoOpen: false,
                dialogClass: 'lookup',
                modal: true,
                resizable: false,
                buttons: {
                    "Close": function () {
                        //Clear comment variables
                        remarkGridID = null;
                        remarkRowID = null;

                        //Close
                        $(this).dialog("close");
                    },
                    "Save & Close": function () {
                        //Save row
                        $('#ccbRemarkSave').click();

                        if (remarkSaved) {
                            //Clear comment variables
                            remarkGridID = null;
                            remarkRowID = null;
                            remarkSaved = null;

                            //Close
                            $(this).dialog("close");
                        }
                    }
                }
            });

            $("#error-dialog").dialog({
                autoOpen: false,
                dialogClass: 'ui-state-error-text',
                modal: true,
                buttons: {
                    "Ok": function () {
                        //Clear message
                        $('#errorMsg').empty();

                        //Close dialog
                        $(this).dialog("close");
                    }
                }
            });

            $("#info-dialog").dialog({
                autoOpen: false,
                dialogClass: 'infoDialog',
                modal: true,
                buttons: {
                    "Ok": function () {
                        //Clear message
                        $('#infoMsg').empty();

                        //Close dialog
                        $(this).dialog("close");
                    }
                }
            });

            $("#wait-dialog").dialog({
                autoOpen: false,
                resizable: false,
                draggable: false,
                dialogClass: 'pleaseWait',
                modal: false
            });
        });

        /* CCB Grid */
        var ordValidation = new Array(); //order validation
        $(function () {
            //Create jqGrid, set options
            $("#listCCB").jqGrid({
                url: "CCB.aspx/getCCBGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { AssetManager: "", Part_no: "", ActivityStatus: "", ViewHistory: "" }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //Get asset manager
                    postData.AssetManager = jQuery('#ccbButton1').prop('value');

                    //Get Part
                    postData.Part_no = jQuery('#ccbButton2').prop('value');

                    //Get Status
                    postData.ActivityStatus = jQuery('#ccbButton3').prop('value');

                    //Get View Scope
                    postData.ViewHistory = $('#toggleHistory span').hasClass("ui-icon-radio-on");

                    //turn post parameters into json string
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                },
                prmNames: {
                    search: null,
                    nd: 'RequestDTStamp',
                    page: 'Page',
                    rows: 'RowLimit',
                    sort: 'SortIndex',
                    order: 'SortOrder'
                },
                ajaxGridOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                loadComplete: function (data) {
                    var mygrid = jQuery("#listCCB")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listCCB")[0];
                    mygrid.grid.hDiv.loading = false;
                    switch (mygrid.p.loadui) {
                        case "disable":
                            break;
                        case "enable":
                            $("#load_" + mygrid.p.id).hide();
                            break;
                        case "block":
                            $("#lui_" + mygrid.p.id).hide();
                            $("#load_" + mygrid.p.id).hide();
                            break;
                    }
                    ErrorHandling(xhr, 'ccb grid ');
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                pager: '#pagerCCB', //Show bottom paging and nav bar
                rowNum: 20, //Default number of rows to display
                rowList: [20, 50, 99], //User options for number of rows to display
                pgbuttons: true, //Show paging buttons
                pginput: true, //Show paging input
                viewrecords: true, //Show total records in pager
                sortable: true, //Allow sorting
                sortname: 'order_no', //Default sort field
                sortorder: 'asc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                multiselect: true,
                caption: '',
                colModel: [
                        { name: 'order_no', label: 'Order', sorttype: 'text', align: 'right' },
                        { name: 'part_no', label: 'Part #', sorttype: 'text', align: 'right' },
                        { name: 'activity_status', label: 'Status', sorttype: 'text', align: 'left', width: 310 },
                        { name: 'cost_charge_number', label: 'CCN', sorttype: 'text', align: 'left' },
                        { name: 'nomenclature', label: 'Nomenclature', sorttype: 'int', align: 'left', width: 200 },
                        { name: 'spo_cost', label: 'Cost', sorttype: 'currency', align: 'right', formatter: 'currency',
                            formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                        },
                        { name: 'order_quantity', label: 'Qty', sorttype: 'int', align: 'right' },
                        { name: 'extended_cost', label: 'Extd. Cost', sorttype: 'currency', align: 'right', formatter: 'currency',
                            formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                        },
                        { name: 'due_date', label: 'Due Date', sorttype: 'date', datefmt: 'm/d/Y', align: 'right' },
                        { name: 'lta', label: 'LTA', sortable: false, align: 'center' },
                        { name: 'pricebreak', sortable: false, editable: false, hidden: true },
                        { name: 'pricePoint', sortable: false, editable: false, hidden: true, formatter: 'currency',
                            formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2}
                        },
                        { name: 'comments', label: 'Comments', sortable: false, align: 'center' }
                       ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "0", //set grid row id by column model zero based index
                    repeatitems: false
                },
                gridComplete: function () {
                    //Variables
                    var $ccbGrid = jQuery('#listCCB');
                    var ord_ids = $ccbGrid.jqGrid('getDataIDs');

                    for (var i = 0; i < ord_ids.length; i++) {
                        btnComments = "<button id='comment_" + ord_ids[i] + "' type='button' class='gridButton' title='View Comment' value='" + ord_ids[i] + "'></button>";

                        var ccbRowData = $ccbGrid.getRowData(ord_ids[i]);

                        //Disable and hide LTA button if no LTA
                        if (ccbRowData.pricebreak == 'false') {
                            //Add buttons to jqGrid
                            $ccbGrid.jqGrid('setRowData', ord_ids[i], { comments: btnComments });
                        }
                        else {
                            btnLta = "<button id='lta_" + ccbRowData.part_no + "' type='button' class='gridButton' title='View LTA' value='" + ccbRowData.part_no + "'>" + ccbRowData.pricePoint + "</button>";

                            //Add buttons to jqGrid
                            $ccbGrid.jqGrid('setRowData', ord_ids[i], { lta: btnLta, comments: btnComments });

                            /* create jQuery button objects in DOM */
                            //View LTA Button
                            $('#' + ord_ids[i] + ' :button[id^="lta_"]').button({
                                text: true
                            })
                            .click(function () {
                                //Remove button highlight
                                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                                
                                //set search field with button value
                                $("#ltaButton2").prop('value', $(this).prop('value'));

                                //reload grid
                                jQuery("#listLta").trigger("reloadGrid");

                                //reload detail
                                reloadLtaPartDetail();

                                //show lta tab
                                $("#tabs").tabs('option', 'active', 1);

                                //Prevent default action after close, no postback
                                return false;
                            });
                        }

                        //View Comment Button
                        $('#' + ord_ids[i] + ' :button[id^="comment_"]').button({
                            icons: {
                                primary: "ui-icon-comment"
                            },
                            text: false
                        })
                            .click(function () {
                                //Remove button highlight
                                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                                
                                //Set Selection
                                remarkGridID = $(this).prop('value');

                                //Open Dialog
                                $("#lookup-comments").dialog("open");

                                //Update grid size and reload grids
                                $("#listAmRemark").setGridWidth($("#amRemarkGridWrapper").width());
                                $("#listAmRemark").setGridHeight(100);
                                jQuery("#listAmRemark").trigger("reloadGrid");

                                $("#listCcbRemark").setGridWidth($("#ccbRemarkGridWrapper").width());
                                $("#listCcbRemark").setGridHeight(100);
                                jQuery("#listCcbRemark").trigger("reloadGrid");

                                $("#listSpoRemark").setGridWidth($("#spoRemarkGridWrapper").width());
                                $("#listSpoRemark").setGridHeight(100);
                                jQuery("#listSpoRemark").trigger("reloadGrid");

                                //Prevent default action after close, no postback
                                return false;
                            });
                    }

                    if (ordValidation.length > 0) {
                        //Process color coding
                        $.each(ord_ids, function (r_index, r_value) {
                            //hold number of validations processed
                            var count = 0;

                            //get order_no
                            var current_ord_num = $ccbGrid.jqGrid('getCell', r_value, 'order_no');

                            //Check if found in array
                            $.each(ordValidation, function (v_index, v_value) {
                                if (v_value.order == current_ord_num) {
                                    //set order row highlight
                                    if (v_value.result == 'true') {
                                        jQuery('#' + r_value + ' td[aria-describedby="listCCB_cost_charge_number"]').removeClass("gridCcnInValid");
                                        jQuery('#' + r_value + ' td[aria-describedby="listCCB_cost_charge_number"]').addClass("gridCcnValid");
                                    }
                                    if (v_value.result == 'false') {
                                        jQuery('#' + r_value + ' td[aria-describedby="listCCB_cost_charge_number"]').removeClass("gridCcnValid");
                                        jQuery('#' + r_value + ' td[aria-describedby="listCCB_cost_charge_number"]').addClass("gridCcnInValid");
                                    }

                                    //add to count, exit loop
                                    count = count + 1;
                                    return false;
                                }
                            });

                            //stop looping rows if we have processed all validation results
                            if (count >= ordValidation.length) return false;
                        });
                    }
                }
            });

            //Add Navigation Bar
            $("#listCCB").jqGrid('navGrid', '#pagerCCB',
            { cloneToTop: true,
                edit: false,
                add: false,
                del: false,
                search: false,
                refresh: true
            }).navButtonAdd('#listCCB_toppager_left', {
                id: "toggleHistory",
                caption: "View More",
                title: "Show Max System Days",
                buttonicon: "ui-icon-radio-off",
                onClickButton: function () {
                    //Switch Icon
                    var $toggleHist = $('#toggleHistory span');
                    var isScopeChecked = $toggleHist.hasClass("ui-icon-radio-on");
                    if (isScopeChecked) {
                        $toggleHist.removeClass("ui-icon-radio-on");
                        $toggleHist.addClass("ui-icon-radio-off");
                    }
                    else {
                        $toggleHist.removeClass("ui-icon-radio-off");
                        $toggleHist.addClass("ui-icon-radio-on");
                    }

                    //Reload grid
                    jQuery("#listCCB").trigger("reloadGrid");
                },
                position: "last"
            }).navSeparatorAdd('#listCCB_toppager_left', {
                sepclass: "",
                sepcontent: "|"
            }).navButtonAdd('#listCCB_toppager_left', {
                caption: "Reject",
                title: "Reject Order(s)",
                buttonicon: "ui-icon-closethick",
                onClickButton: function () {
                    //Remove button highlight
                    $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");

                    //Variables
                    var $ccbGrid = jQuery('#listCCB');

                    //get all selected row ids
                    var selectedOrderRows = $ccbGrid.jqGrid('getGridParam', 'selarrrow');

                    //Check number of rows in grid
                    var rowCount = $ccbGrid.getGridParam("reccount");

                    //If only one row exists, auto-select row for user
                    if (rowCount == 1) {
                        //get first row id
                        var firstRowId = $ccbGrid.jqGrid('getDataIDs')[0];

                        //select row if not already selected
                        if (selectedOrderRows[0] != firstRowId) {
                            $ccbGrid.jqGrid('setSelection', firstRowId);
                        }
                    }

                    //process selections to get unique key
                    if (selectedOrderRows.length > 0) {
                        //Open processing dialog
                        setTimeout(function () {
                            $("#wait-dialog").dialog("open");
                        }, 0);

                        //send ajax request
                        $.ajax({
                            url: "CCB.aspx/multiEdit",
                            dataType: "json",
                            type: 'POST',
                            contentType: 'application/json; charset=utf-8',
                            data: JSON.stringify({ oper: 'reject', orders: selectedOrderRows }),
                            success: function (data, textStatus, jqXHR) {
                                //Close processing dialog
                                setTimeout(function () {
                                    $("#wait-dialog").dialog("close");
                                }, 0);

                                //Show errors
                                if (data.d.hasError) {
                                    var ers = "";

                                    //prepare each error in array for display
                                    $.each(data.d.errorList, function (index, value) {
                                        ers += value + '<br />';
                                    });

                                    //Show info dialog
                                    showErrorDialog(ers);
                                }

                                //Show messages
                                if (data.d.hasMsg) {
                                    var msg = "";

                                    //prepare each msg in array for display
                                    $.each(data.d.msgList, function (index, value) {
                                        msg += value + '<br />';
                                    });

                                    //Show info dialog
                                    showInfoDialog(msg);
                                }

                                return true;
                            },
                            error: function (jqXHR, status, error) {
                                //Close processing dialog
                                setTimeout(function () {
                                    $("#wait-dialog").dialog("close");
                                }, 0);

                                //Show error dialog
                                ErrorHandling(jqXHR, "ccb multi edit");
                                return false;
                            }
                        });
                    }
                    else {
                        //Show error dialog
                        showErrorDialog('Please select an order.');
                    }

                    //Reload grid
                    $ccbGrid.trigger("reloadGrid");

                    //Prevent default action after close, no postback
                    return false;
                },
                position: "last"
            }).navButtonAdd('#listCCB_toppager_left', {
                caption: "Approve",
                title: "Approve Order(s)",
                buttonicon: "ui-icon-check",
                onClickButton: function () {
                    //Remove button highlight
                    $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");

                    //Variables
                    var $ccbGrid = jQuery('#listCCB');

                    //get all selected row ids
                    var selectedOrderRows = $ccbGrid.jqGrid('getGridParam', 'selarrrow');

                    //Check number of rows in grid
                    var rowCount = $ccbGrid.getGridParam("reccount");

                    //If only one row exists, auto-select row for user
                    if (rowCount == 1) {
                        //get first row id
                        var firstRowId = $ccbGrid.jqGrid('getDataIDs')[0];

                        //select row if not already selected
                        if (selectedOrderRows[0] != firstRowId) {
                            $ccbGrid.jqGrid('setSelection', firstRowId);
                        }
                    }

                    //process selections to get unique key
                    if (selectedOrderRows.length > 0) {
                        //Open processing dialog
                        setTimeout(function () {
                            $("#wait-dialog").dialog("open");
                        }, 0);

                        //send ajax request
                        $.ajax({
                            url: "CCB.aspx/multiEdit",
                            dataType: "json",
                            type: 'POST',
                            contentType: 'application/json; charset=utf-8',
                            data: JSON.stringify({ oper: 'approve', orders: selectedOrderRows }),
                            success: function (data, textStatus, jqXHR) {
                                //Close processing dialog
                                setTimeout(function () {
                                    $("#wait-dialog").dialog("close");
                                }, 0);

                                //Show .net errors
                                if (data.d.hasError) {
                                    var ers = "";

                                    //prepare each error in array for display
                                    $.each(data.d.errorList, function (index, value) {
                                        ers += value + '<br />';
                                    });

                                    //Show info dialog
                                    showErrorDialog(ers);
                                }

                                //Show .net messages
                                if (data.d.hasMsg) {
                                    var msg = "";

                                    //prepare each msg in array for display
                                    $.each(data.d.msgList, function (index, value) {
                                        msg += value + '<br />';
                                    });

                                    //Show info dialog
                                    showInfoDialog(msg);
                                }

                                //Process .net return data
                                ordValidation = new Array();
                                if (data.d.hasReturnData) {
                                    //save data to global variable / process after grid reload
                                    $.each(data.d.returnData, function (index, value) {
                                        ordValidation.push({ order: index, result: value });
                                    });
                                }

                                return true;
                            },
                            error: function (jqXHR, status, error) {
                                //close processing dialog
                                setTimeout(function () {
                                    $("#wait-dialog").dialog("close");
                                }, 0);

                                //Show error dialog
                                ErrorHandling(jqXHR, " ccb multi edit ");
                                return false;
                            }
                        });
                    }
                    else {
                        //Show error dialog
                        showErrorDialog('Please select an order.');
                    }

                    //Reload grid
                    $ccbGrid.trigger("reloadGrid");

                    //Prevent default action after close, no postback
                    return false;
                },
                position: "last"
            });
        });

        /* Asset Manager Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listManager").jqGrid({
                url: "CCB.aspx/getManGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { AssetManager: "", ViewHistory: "" }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //Get asset manager
                    postData.AssetManager = jQuery('#ccbButton1').prop('value');

                    //Get View Scope
                    postData.ViewHistory = $('#toggleHistory span').hasClass("ui-icon-radio-on");

                    //turn post parameters into json string
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                },
                prmNames: {
                    search: null,
                    nd: 'RequestDTStamp',
                    page: null,
                    rows: null,
                    sort: null,
                    order: null
                },
                ajaxGridOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                loadComplete: function (data) {
                    var mygrid = jQuery("#listManager")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listManager")[0];
                    mygrid.grid.hDiv.loading = false;
                    switch (mygrid.p.loadui) {
                        case "disable":
                            break;
                        case "enable":
                            $("#load_" + mygrid.p.id).hide();
                            break;
                        case "block":
                            $("#lui_" + mygrid.p.id).hide();
                            $("#load_" + mygrid.p.id).hide();
                            break;
                    }
                    ErrorHandling(xhr, " asset manager lookup ");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                //pager: '#pagerManager', //Show bottom paging and nav bar
                rowNum: 999999, //Default number of rows to display
                //rowList: [10, 20, 30], //User options for number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: true, //Show total records in pager
                sortable: false, //Allow sorting
                sortname: 'asset_manager_id', //Default sort field
                sortorder: 'desc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                caption: '',
                colModel: [
                        { name: 'asset_manager_id', label: 'ID', sorttype: 'text', align: 'center' },
                        { name: 'asset_manager_id_name', label: 'Asset Manager', sortable: false, sorttype: 'text', align: 'left' }
                      ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "0", //set grid row id by column model zero based index
                    repeatitems: false
                }
            });

            //Add Navigation Bar
            $("#listManager").jqGrid('navGrid', '#pagerManager',
            { cloneToTop: true,
                edit: false,
                add: false,
                del: false,
                search: false,
                refresh: true
            }
        );
        });

        /* Part Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listPart").jqGrid({
                url: "CCB.aspx/getPrtGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { Part_no: "", LtaPart: "false", ViewHistory: "" }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //get part number
                    if (flagLtaPartGrid) {
                        postData.Part_no = jQuery('#ltaButton2').prop('value');
                        postData.LtaPart = "true";
                    }
                    else {
                        postData.Part_no = jQuery('#ccbButton2').prop('value');
                        postData.LtaPart = "false";
                    }

                    //Get View Scope
                    postData.ViewHistory = $('#toggleHistory span').hasClass("ui-icon-radio-on");

                    //turn post parameters into json string
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                },
                prmNames: {
                    search: null,
                    nd: 'RequestDTStamp',
                    page: 'Page',
                    rows: 'RowLimit',
                    sort: 'SortIndex',
                    order: 'SortOrder'
                },
                ajaxGridOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                loadComplete: function (data) {
                    var mygrid = jQuery("#listPart")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listPart")[0];
                    mygrid.grid.hDiv.loading = false;
                    switch (mygrid.p.loadui) {
                        case "disable":
                            break;
                        case "enable":
                            $("#load_" + mygrid.p.id).hide();
                            break;
                        case "block":
                            $("#lui_" + mygrid.p.id).hide();
                            $("#load_" + mygrid.p.id).hide();
                            break;
                    }
                    ErrorHandling(xhr, " part lookup ");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: false, //Show top paging and nav bar
                pager: '#pagerPart', //Show bottom paging and nav bar
                rowNum: 50, //Default number of rows to display
                //rowList: [50], //User options for number of rows to display
                pgbuttons: true, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: true, //Show total records in pager
                sortable: true, //Allow sorting
                sortname: 'part_no', //Default sort field
                sortorder: 'asc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                caption: 'Parts',
                colModel: [
                            { name: 'part_no', label: 'Part No', sorttype: 'text', align: 'center' },
                      ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "0", //set grid row id by column model zero based index
                    repeatitems: false
                }
            });

            //Add Navigation Bar
            $("#listPart").jqGrid('navGrid', '#pagerPart',
                { cloneToTop: true,
                    edit: false,
                    add: false,
                    del: false,
                    search: false,
                    refresh: true
                }
            );

            //Hide Grid title bar
            $("#gview_listPart > .ui-jqgrid-titlebar").hide();
        });
        
        /* Activity Status Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listActivity").jqGrid({
                url: "CCB.aspx/getActivityStatusGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: {}, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //turn post parameters into json string
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                },
                prmNames: {
                    search: null,
                    nd: 'RequestDTStamp',
                    page: 'Page',
                    rows: 'RowLimit',
                    sort: 'SortIndex',
                    order: 'SortOrder'
                },
                ajaxGridOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                loadComplete: function (data) {
                    var mygrid = jQuery("#listActivity")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listActivity")[0];
                    mygrid.grid.hDiv.loading = false;
                    switch (mygrid.p.loadui) {
                        case "disable":
                            break;
                        case "enable":
                            $("#load_" + mygrid.p.id).hide();
                            break;
                        case "block":
                            $("#lui_" + mygrid.p.id).hide();
                            $("#load_" + mygrid.p.id).hide();
                            break;
                    }
                    ErrorHandling(xhr, " activity status ");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                //pager: '#pagerCCB', //Show bottom paging and nav bar            
                rowNum: 999999, //Default number of rows to display
                //rowList: [10, 20, 30], //User options for number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: true, //Show total records in pager
                sortable: true, //Allow sorting
                sortname: 'activity_status', //Default sort field
                sortorder: 'asc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                caption: 'Activity Status',
                colModel: [
                        { name: 'activity_status', label: 'Activity Status', sortable: false, align: 'center' }
                      ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "0", //set grid row id by column model zero based index
                    repeatitems: false
                }
            });

            //Add Navigation Bar
            $("#listActivity").jqGrid('navGrid', '#pagerActivity',
                { cloneToTop: true,
                    edit: false,
                    add: false,
                    del: false,
                    search: false,
                    refresh: true
                }
            );
            //Hide Grid title bar
            $("#gview_listActivity > .ui-jqgrid-titlebar").hide();
        });

        /* AM Remark Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listAmRemark").jqGrid({
                url: "CCB.aspx/getAmRemarkGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { OrderNo: "0" }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //Get order number
                    if (remarkGridID) {
                        postData.OrderNo = remarkGridID;
                    }

                    //turn post parameters into json string
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                },
                prmNames: {
                    search: null,
                    nd: 'RequestDTStamp',
                    page: null,
                    rows: null,
                    sort: null,
                    order: null
                },
                ajaxGridOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                loadComplete: function (data) {
                    var mygrid = jQuery("#listAmRemark")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listAmRemark")[0];
                    mygrid.grid.hDiv.loading = false;
                    switch (mygrid.p.loadui) {
                        case "disable":
                            break;
                        case "enable":
                            $("#load_" + mygrid.p.id).hide();
                            break;
                        case "block":
                            $("#lui_" + mygrid.p.id).hide();
                            $("#load_" + mygrid.p.id).hide();
                            break;
                    }
                    ErrorHandling(xhr, " asset manager remarks ");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: false, //Show top paging and nav bar
                //pager: '#pagerCCB', //Show bottom paging and nav bar
                rowNum: 999999, //Default number of rows to display
                //rowList: [10, 20, 30], //User options for number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: false, //Show total records in pager
                sortable: false, //Allow sorting
                //sortname: 'order_no', //Default sort field
                //sortorder: 'asc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                caption: 'Asset Manager Remark',
                colModel: [
                                { name: 'order_no', sortable: false, hidden: true },
                                { name: 'asset_manager_remark', label: 'Remarks', sortable: false, align: 'left',
                                    cellattr: function (rowId, tv, rawObject, cm, rdata) { return 'style="white-space: normal !important;"' }
                                }
                          ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "0", //set grid row id by column model zero based index
                    repeatitems: false
                }
            });

            //Hide Grid column headers
            $("#gview_listAmRemark > .ui-jqgrid-hdiv").hide();
        });

        /* CCB Remark Grid */
        var remarkRowID;
        var remarkSaved;
        $(function () {
            //Create jqGrid, set options
            $("#listCcbRemark").jqGrid({
                url: "CCB.aspx/getCcbRemarkGrid",
                editurl: "CCB.aspx/editCcbRemark",
                datatype: 'json',
                mtype: 'POST',
                postData: { OrderNo: "0" }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //Get order number
                    if (remarkGridID) {
                        postData.OrderNo = remarkGridID;
                    }

                    //turn post parameters into json string
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                },
                prmNames: {
                    search: null,
                    nd: 'RequestDTStamp',
                    page: null,
                    rows: null,
                    sort: null,
                    order: null
                },
                ajaxGridOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                loadComplete: function (data) {
                    var mygrid = jQuery("#listCcbRemark")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listCcbRemark")[0];
                    mygrid.grid.hDiv.loading = false;
                    switch (mygrid.p.loadui) {
                        case "disable":
                            break;
                        case "enable":
                            $("#load_" + mygrid.p.id).hide();
                            break;
                        case "block":
                            $("#lui_" + mygrid.p.id).hide();
                            $("#load_" + mygrid.p.id).hide();
                            break;
                    }
                    ErrorHandling(xhr, " ccb remarks ");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                //pager: '#pagerCcbRemark', //Show bottom paging and nav bar
                rowNum: 999999, //Default number of rows to display
                //rowList: [10, 20, 30], //User options for number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: false, //Show total records in pager
                sortable: false, //Allow sorting
                //sortname: 'order_no', //Default sort field
                //sortorder: 'asc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                caption: 'Review Board Remark',
                colModel: [
                                { name: 'order_no', sortable: false, hidden: true },
                                { name: 'review_board_remark', label: 'Remarks', sortable: false, align: 'left',
                                    editable: true, edittype: 'textarea', editoptions: { rows: "5" }
                                }
                          ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "0", //set grid row id by column model zero based index
                    repeatitems: false
                },
                gridComplete: function () {
                    //get row ids
                    var remark_ids = jQuery("#listCcbRemark").jqGrid('getDataIDs');

                    //If data exists
                    if (remark_ids[0]) {
                        //select first row, call onselectrow
                        jQuery("#listCcbRemark").setSelection(remark_ids[0], true);
                    }
                },
                onSelectRow: function (rowid, status) {
                    jQuery('#listCcbRemark').jqGrid('editRow', rowid, true);

                    //save row id to variable for dialog to call correct row when saving
                    remarkRowID = rowid;
                },
                ajaxRowOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                serializeRowData: function (postData) {
                    //Convert JSON to JSONstring
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                }
            });

            //Add Navigation Bar
            $("#listCcbRemark").jqGrid('navGrid', '#pagerCcbRemark',
                            { cloneToTop: true,
                                edit: false,
                                add: false,
                                del: false,
                                search: false,
                                refresh: true
                            }
            ).navButtonAdd('#listCcbRemark_toppager_left', {
                id: "ccbRemarkSave",
                caption: "",
                title: "Save",
                buttonicon: "ui-icon-disk",
                onClickButton: function () {
                    //Save row when a row selected
                    if (remarkRowID) {
                        var rData = $("#" + remarkRowID + "_review_board_remark").prop('value');
                        var rValidationResults = validateCcbRemark(rData);

                        //save row when it contains valid input
                        if (!rValidationResults[0]) {
                            //Show error dialog
                            showErrorDialog(rValidationResults[1]);
                        }
                        else {
                            //Save, check response for data save error
                            $('#listCcbRemark').jqGrid('saveRow', remarkRowID,
                            function (data) {
                                var response = $.parseJSON(data.responseText);
                                if (response.d.hasError) {
                                    var ers = "";

                                    //prepare each error in array for display
                                    $.each(response.d.errorList, function (index, value) {
                                        ers += value + '<br />';
                                    });

                                    //Show error dialog
                                    showErrorDialog(ers);

                                    return false;
                                }
                                remarkSaved = true;
                                return true;
                            }
                        );

                            jQuery('#listCcbRemark').trigger('reloadGrid');
                        }
                    }

                    //Prevent default action of following link
                    return false;
                },
                position: "last"
            });

            //Hide Grid column headers
            $("#gview_listCcbRemark > .ui-jqgrid-hdiv").hide();
        });

        /* LTA Part Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listLta").jqGrid({
                url: "CCB.aspx/getLtaGrid",
                datatype: 'json',
                mtype: 'POST',
                serializeGridData: function (postData) {
                    //get part number from inputbox
                    postData.SearchValue = $("#ltaButton2").prop('value');
                    if (!postData.SearchValue) {
                        postData.SearchValue = '';
                    }

                    //turn post parameters into json string
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                },
                prmNames: {
                    search: 'SearchValue',
                    nd: 'RequestDTStamp',
                    page: null,
                    rows: null,
                    sort: 'SortIndex',
                    order: 'SortOrder'
                },
                ajaxGridOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                loadComplete: function (data) {
                    var mygrid = jQuery("#listLta")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listLta")[0];
                    mygrid.grid.hDiv.loading = false;
                    switch (mygrid.p.loadui) {
                        case "disable":
                            break;
                        case "enable":
                            $("#load_" + mygrid.p.id).hide();
                            break;
                        case "block":
                            $("#lui_" + mygrid.p.id).hide();
                            $("#load_" + mygrid.p.id).hide();
                            break;
                    }
                    ErrorHandling(xhr, "LTA");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: false, //Show top paging and nav bar
                //pager: '#pagerLta', //Show bottom paging and nav bar
                rowNum: 999999, //Default number of rows to display
                //rowList: [10, 20, 30], //User options for number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: false, //Show total records in pager
                sortable: false, //Allow sorting
                //sortname: 'end_price_break_calendar_date', //Default sort field
                //sortorder: 'asc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                caption: '',
                colModel: [
                            { name: 'id', key: true, hidden: true },
                            { name: 'lta', label: 'LTA', sorttype: 'text', sortable: false },
                            { name: 'expired_calendar_date', label: 'LTA Expired', sorttype: 'date', datefmt: 'm/d/Y', sortable: false },
                            { name: 'start_price_break_cal_date', label: 'Price Start', sorttype: 'date', datefmt: 'm/d/Y', sortable: false },
                            { name: 'end_price_break_calendar_date', label: 'Price End', sorttype: 'date', datefmt: 'm/d/Y', sortable: false },
                            { name: 'price_break_start_qty', label: 'Start Qty', sorttype: 'int', sortable: false },
                            { name: 'price_break_end_qty', label: 'End Qty', sorttype: 'int', sortable: false },
                            { name: 'unit_price', label: 'Unit Price', sorttype: 'currency', sortable: false, formatter: 'currency',
                                formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                            }
                        ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "id", //set grid row id by column model zero based index
                    repeatitems: false
                },
                grouping: true,
                groupingView: {
                    groupField: ['lta'],
                    groupColumnShow: [false],
                    groupText: ['LTA: <b>{0}</b>'],
                    groupCollapse: false,
                    groupOrder: ['asc'],
                    groupSummary: [false],
                    groupDataSorted: true
                }
            });
        });

        /* Summary Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listSummary").jqGrid({
                url: "CCB.aspx/getSumryGrid",
                datatype: 'json',
                mtype: 'POST',
                serializeGridData: function (postData) {
                    //get date interval
                    postData.startDate = $("#sumStartDate").prop('value');
                    postData.endDate = $("#sumEndDate").prop('value');

                    //turn post parameters into json string
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                },
                prmNames: {
                    search: null,
                    nd: 'RequestDTStamp',
                    page: null,
                    rows: null,
                    sort: null,
                    order: null
                },
                ajaxGridOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                loadComplete: function (data) {
                    var mygrid = jQuery("#listSummary")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listSummary")[0];
                    mygrid.grid.hDiv.loading = false;
                    switch (mygrid.p.loadui) {
                        case "disable":
                            break;
                        case "enable":
                            $("#load_" + mygrid.p.id).hide();
                            break;
                        case "block":
                            $("#lui_" + mygrid.p.id).hide();
                            $("#load_" + mygrid.p.id).hide();
                            break;
                    }

                    ErrorHandling(xhr, "summary grid");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: false, //Show top paging and nav bar           
                rowNum: 999999, //Default number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: true, //Show total records in pager
                sortable: false, //Allow sorting
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                caption: '',
                colModel: [
                        { name: 'program_code', label: 'Program', sorttype: 'text', align: 'center', sortable: false },
                        { name: 'total_accept_part_count', label: 'Accept Count', sorttype: 'int', align: 'right', sortable: false },
                        { name: 'total_accept_part_cost', label: 'Accept Cost', sorttype: 'currency', sortable: false, align: 'right',
                            formatter: 'currency', formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                        },
                        { name: 'total_reject_part_count', label: 'Reject Count', sorttype: 'int', align: 'right', sortable: false },
                        { name: 'total_reject_part_cost', label: 'Reject Cost', sorttype: 'currency', sortable: false, align: 'right',
                            formatter: 'currency', formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                        },
                        { name: 'total_part_count', label: 'Total Part Count', sorttype: 'int', align: 'right', sortable: false },
                        { name: 'total_part_cost', label: 'Total Part Cost', sorttype: 'currency', sortable: false, align: 'right',
                            formatter: 'currency', formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                        }
                      ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "0", //set grid row id by column model zero based index
                    repeatitems: false
                },
                multiselect: false,
                subGrid: true,
                subGridRowExpanded: function (subgrid_id, row_id) {
                    // we pass two parameters
                    // subgrid_id is a id of the div tag created within a table
                    // the row_id is the id of the row
                    // If we want to pass additional parameters to the url we can use
                    // the method getRowData(row_id) - which returns associative array in type name-value
                    // here we can easy construct the following
                    var subgrid_table_id;
                    subgrid_table_id = subgrid_id + "_t";
                    jQuery("#" + subgrid_id).html("<table id='" + subgrid_table_id + "' class='scroll'></table>");
                    jQuery("#" + subgrid_table_id).jqGrid({
                        url: "CCB.aspx/getSumryReasonGrid",
                        datatype: 'json',
                        mtype: 'POST',
                        serializeGridData: function (postData) {
                            //get date interval
                            postData.startDate = $("#sumStartDate").prop('value');
                            postData.endDate = $("#sumEndDate").prop('value');
                            postData.programCode = row_id;

                            //turn post parameters into json string
                            var postDataString = JSON.stringify(postData);
                            return postDataString;
                        },
                        prmNames: {
                            search: null,
                            nd: 'RequestDTStamp',
                            page: null,
                            rows: null,
                            sort: null,
                            order: null
                        },
                        ajaxGridOptions: {
                            contentType: "application/json; charset=utf-8"
                        },
                        loadComplete: function (data) {
                            var mygrid = jQuery("#" + subgrid_table_id)[0];
                            mygrid.addJSONData(data.d);
                        },
                        loadError: function (xhr, status, error) {
                            var mygrid = jQuery("#" + subgrid_table_id)[0];
                            mygrid.grid.hDiv.loading = false;
                            switch (mygrid.p.loadui) {
                                case "disable":
                                    break;
                                case "enable":
                                    $("#load_" + mygrid.p.id).hide();
                                    break;
                                case "block":
                                    $("#lui_" + mygrid.p.id).hide();
                                    $("#load_" + mygrid.p.id).hide();
                                    break;
                            }

                            ErrorHandling(xhr, "summary sub grid");
                        },
                        loadui: 'block', //Loading message options
                        autowidth: false,
                        height: '100%',
                        toppager: false, //Show top paging and nav bar           
                        rowNum: 999999, //Default number of rows to display
                        pgbuttons: false, //Show paging buttons
                        pginput: false, //Show paging input
                        viewrecords: false, //Show total records in pager
                        sortable: false, //Allow sorting
                        gridview: true,
                        hidegrid: false,
                        rownumbers: false,
                        caption: '',
                        colModel: [
                                    { name: 'program_code', hidden: true },
                                    { name: 'spacer', label: ' ', width: 30 },
                                    { name: 'change_code', label: 'Reason', sorttype: 'text', align: 'left', sortable: false, width: 160 },
                                    { name: 'accept_part_count', label: 'Accept Count', sorttype: 'int', align: 'right', sortable: false, width: 120 },
                                    { name: 'accept_part_cost', label: 'Accept Cost', sorttype: 'currency', align: 'right', sortable: false, width: 120,
                                        formatter: 'currency', formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                                    },
                                    { name: 'reject_part_count', label: 'Reject Count', sorttype: 'int', align: 'right', sortable: false, width: 120 },
                                    { name: 'reject_part_cost', label: 'Reject Cost', sorttype: 'currency', align: 'right', sortable: false, width: 120,
                                        formatter: 'currency', formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                                    }
                                  ],
                        jsonReader: {
                            root: "rows",
                            page: "currentPage",
                            total: "totalPages",
                            records: "totalRows",
                            id: "1", //set grid row id by column model zero based index
                            repeatitems: false
                        }
                    });
                }
            });
        });

        /* Shared Functions */
        //Set by comment button upon click, shared by all comment grids
        var remarkGridID;

        //Flag denotes which grid to update part info with
        var flagLtaPartGrid = false;

        //Delay grid resize on window resize
        var resizeTimer;
        $(window).bind('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(resizeGrids, 60);
        });

        //Resize grids by tab index
        function resizeGrids() {
            var currentTab = getCurrentTabIndex();
            if (currentTab == 0) {
                var $ccbGrid = jQuery('#listCCB');
                $ccbGrid.setGridWidth($("#ccbGridWrapper").width());
            }
            else if (currentTab == 1) {
                var $ltaGrid = jQuery("#listLta");
                $ltaGrid.setGridWidth($("#ltaGridWrapper").width());
            }
            else if (currentTab == 2) {
                var $summaryGrid = jQuery("#listSummary");
                $summaryGrid.setGridWidth($("#summaryGridWrapper").width());
            }
        };

        //Reload part detail info on lta report
        function reloadLtaPartDetail() {
            //Clear detail
            $('[id ^= ltaDtl_]').empty();

            //get part number from inputbox
            var prt = $("#ltaButton2").prop('value');
            if (!prt) prt = '';

            //update page
            $("#ltaDtl_part").append(prt);

            //get part detail
            $.ajax({
                url: "CCB.aspx/getLtaPartInfo",
                dataType: "json",
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ part: prt }),
                success: function (data, status, jqXHR) {
                    //update page
                    $("#ltaDtl_description").append(data.d.description);
                    $("#ltaDtl_plo").append(data.d.plo_qty);
                    $("#ltaDtl_pr").append(data.d.pr_qty);
                    $("#ltaDtl_forecast").append(data.d.forecast_qty);

                    return true;
                },
                error: function (jqXHR, status, error) {
                    //Show error dialog
                    ErrorHandling(jqXHR, "lta part detail. ");
                    return false;
                }
            });
        };

        /* Custom Validations */
        //validate ccb remark
        function validateCcbRemark(value) {
            if (value.length > 200) {
                return [false, "Max remark length is 200 characters."];
            }

            if (value) {
                var ccbRemarkRegex = /[<>'"]/;
                if (ccbRemarkRegex.test(value)) {
                    return [false, "The following characters are not allowed: <, >, single and double quotes."];
                }
            }

            return [true, ""];
        };
</script>
</asp:Content>
