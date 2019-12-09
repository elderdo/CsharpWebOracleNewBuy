<%@ Page Title="UK Activity" Language="C#" MasterPageFile="~/NewBuy.Master" AutoEventWireup="true" CodeBehind="Activity.aspx.cs" Inherits="NewBuy.UK.Activity" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div id="tabs">
    <ul>
        <li><a href="#tabs-Requirement">Requirements</a></li>
        <li><a href="#tabs-Order">Orders</a></li>
        <li><a href="#tabs-Part-LTA">Part LTA</a></li>
    </ul>
    <div id="tabs-Requirement">
        <div class="toolbar">
            <button id="reqButton1" title="Asset Manager" value="">Asset Manager</button>
            <button id="reqButton2" title="Part Number" value="">Part Number</button>
            <button id="reqButton4" title="My Export" value="">My Export</button>
            <div id="budget-Data" class="toolbar-inset-right">
                Budget Total: <asp:Label ID="BudgetTotal" runat="server" Text=""></asp:Label>  |  
                Budgeted Spent: <asp:Label ID="BudgetedSpent" runat="server" Text=""></asp:Label>  |  
                Non-Budgeted Spent: <asp:Label ID="NonBudgetedSpent" runat="server" Text=""></asp:Label><br />
                Total Spent: <asp:Label ID="BudgetSpentPercent" runat="server" Text=""></asp:Label>  |  
                Remaining: <asp:Label ID="BudgetRemain" runat="server" Text=""></asp:Label>                
            </div>
        </div>
        <div id="reqGridWrapper" class="ui-jqgrid gridWrapper">
            <table id="listReq"></table>
            <div id="pagerReq"></div>
        </div>
    </div>
    <div id="tabs-Order" class="ui-tabs-hide">
        <div id="orderGridWrapper" class="ui-jqgrid gridWrapper">
            <table id="listOrder"></table>
            <div id="pagerOrd"></div>
        </div>
    </div>
    <div id="tabs-Part-LTA" class="ui-tabs-hide">
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
<div id="lookup-variance" title="Submit a Comment">
    <div id="varianceGridWrapper" class="ui-jqgrid gridWrapper">
        <table id="listVariance"></table>
        <div id="pagerVariance"></div>
    </div>
</div>
<div id="lookup-comments" title="Order Comments">
    <div id="amRemarkGridWrapper" class="ui-jqgrid gridWrapper">
        <div>Not Allowed: <, >, ', "</div>
        <table id="listAmRemark"></table>
        <div id="pagerAmRemark"></div>
    </div><br />
    <div id="ccbRemarkGridWrapper" class="ui-jqgrid gridWrapper">
        <table id="listCcbRemark"></table>
        <div id="pagerCcbRemark"></div>
    </div>
</div>
<div id="lookup-selectedReq" title="Select a Requirement">
    <div id="selectedReqGridWrapper" class="ui-jqgrid gridWrapper">
        <table id="listSelectedReq"></table>
        <div id="pagerSelectedReq"></div>
    </div>
</div>
<div id="confirm-order-delete" title="Delete this Order?">
    <p><span class="ui-icon ui-icon-alert dialogIcon"></span><span id="confirmDeleteMsg"></span></p>
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
        jQuery(function () {
            //Tabs
            $("#tabs").tabs
            ({
                activate: function (event, ui) {
                    //Resize grid
                    resizeGrids();

                    //Order Tab if Req Changed
                    var activeTab = $(this).tabs("option", "active");
                    if (activeTab == 1 && reqMultiSelChanged) {
                        //Check for editing
                        var ord_edit_id = jQuery('#listOrder tr[editable="1"]').prop('id');

                        //Cancel current edit row
                        if (ord_edit_id) {
                            jQuery('#cancel_' + ord_edit_id.toString()).click();
                            ord_edit_id = null;
                        }

                        //Reload grid, reset flag
                        jQuery("#listOrder").trigger("reloadGrid");
                        reqMultiSelChanged = false;
                    }
                }
            });

            //Buttons
            //Asset Manager lookup
            $("#reqButton1").button({
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

            //Part lookup (requirements tab)
            $("#reqButton2").button({
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

            //Prevent default action after close, no postback
            return false;
        });

            //My Export
            $("#reqButton4").button({
                icons: {
                    primary: "ui-icon-heart"
                },
                text: true
            })
        .click(function () {
            //Remove button highlight
            $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
            
            //Clear all other buttons
            jQuery('[id ^= reqButton]').prop('value', '');

            //Reload grid
            jQuery("#listReq").trigger("reloadGrid");

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
                            jQuery('[id ^= reqButton]').prop('value', '');

                            //Set button value
                            jQuery('#reqButton1').prop('value', mangrid_sel_id);

                            //Reload grid
                            jQuery("#listReq").trigger("reloadGrid");

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
                    if (!flagLtaPartGrid) $('#reqButton2').prop('value', '');

                    //update grid size
                    $("#listPart").setGridWidth($("#partGridWrapper").width());
                    $("#listPart").setGridHeight(300);

                    //reload grid
                    jQuery("#listPart").trigger("reloadGrid");

                    //reload grid as user types
                    $("#partSrcText").keyup(function () {
                        //set button
                        if (flagLtaPartGrid) $('#ltaButton2').prop('value', $("#partSrcText").prop('value'));
                        if (!flagLtaPartGrid) $('#reqButton2').prop('value', $("#partSrcText").prop('value'));

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
                                jQuery('[id ^= reqButton]').prop('value', '');

                                //Set button value
                                jQuery('#reqButton2').prop('value', prtgrid_sel_id);

                                //Reload grid
                                jQuery("#listReq").trigger("reloadGrid");
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

            // Flag used by lookup-variance dialog... prevents premature closure
            var flagSubmitted = false;
            // Lookup box listing Change Reason options
            $("#lookup-variance").dialog({
                autoOpen: false,
                dialogClass: 'lookup',
                modal: true,
                resizable: false,
                open: function (event, ui) {
                    $(".ui-dialog-titlebar-close", this.parentNode).hide();
                },
                buttons: {
                    "Submit": function () {
                        //Releases dialog box for closure
                        flagSubmitted = true;
                        if (ordJustificationIds.length > 0) {
                            var $varGrid = $('#listVariance');

                            //Get selected comment for change reason
                            var var_sel_id = $varGrid.jqGrid('getGridParam', 'selrow');
                            var var_cmt_sel_data = $varGrid.jqGrid('getCell', var_sel_id, 'comment');

                            //Send ajax request - update asset_manager_remarks field in requirements table
                            $.ajax({
                                url: "Activity.aspx/editNBVarianceComment",
                                dataType: "json",
                                type: 'POST',
                                contentType: 'application/json; charset=utf-8',
                                data: JSON.stringify({ internalOrders: ordJustificationIds, comment: var_cmt_sel_data }),
                                error: function (jqXHR, status, error) {
                                    //Show error dialog
                                    ErrorHandling(jqXHR, "edit new buy variance comment. ");
                                    return false;
                                }
                            });
                        }

                        jQuery('#listOrder').trigger("reloadGrid");

                        //Close
                        $(this).dialog("close");
                    }
                },
                beforeclose: function () { return flagSubmitted; }
            });

            //When adding order, select requirement
            $("#lookup-selectedReq").dialog({
                autoOpen: false,
                dialogClass: 'lookupWide',
                modal: true,
                resizable: false,
                open: function (event, ui) {
                    //Variables
                    var $selReqGrid = jQuery("#listSelectedReq");
                    var selectedOrderRows = $selReqGrid.jqGrid('getGridParam', 'selarrrow');

                    //Check number of rows in grid
                    var rowCount = $selReqGrid.getGridParam("reccount");

                    //If only one row exists, auto-select row for user
                    if (rowCount == 1) {
                        //get first row id
                        var firstRowId = $selReqGrid.jqGrid('getDataIDs')[0];

                        //select row if not already selected
                        if (selectedOrderRows[0] != firstRowId) {
                            $selReqGrid.jqGrid('setSelection', firstRowId);
                        }

                        //Click dialog select button to auto-continue
                        jQuery('#lookup_selectedReq_SelectBtn').click();
                    }
                },
                buttons: [
                    {
                        text: "Select",
                        id: "lookup_selectedReq_SelectBtn",
                        click: function () {
                            var selreq_sel_id = jQuery("#listSelectedReq").jqGrid('getGridParam', 'selrow');
                            if (selreq_sel_id) {
                                //create new row in grid
                                jQuery("#listOrder").jqGrid('addRow', { rowID: "new_row", initdata: { change_reason: 'Capacity Split Orders'} });

                                //Click edit button for user
                                jQuery('#edit_new_row').click();

                                //Close
                                $(this).dialog("close");
                            }
                        }
                    },
                    {
                        text: "Cancel",
                        click: function () {
                            //Close
                            $(this).dialog("close");
                        }
                    }
                ]
            });

            $("#confirm-order-delete").dialog({
                autoOpen: false,
                dialogClass: 'confirm',
                modal: true,
                open: function (event) {
                    //Get all selected orders
                    var selectedOrdRowIDs = $("#listOrder").jqGrid('getGridParam', 'selarrrow');

                    //Add message
                    $('#confirmDeleteMsg').append("Delete " + selectedOrdRowIDs.length.toString() + " selected item(s)?");
                },
                buttons: {
                    "Confirm Delete": function () {
                        //Get all selected orders
                        var selectedOrdRowIDs = $("#listOrder").jqGrid('getGridParam', 'selarrrow');

                        //process selections to get unique key
                        if (selectedOrdRowIDs.length > 0) {
                            //instantiate variable to hold row keys
                            ordMultiSel = new Array();

                            //get the key for each row selected
                            $.each(selectedOrdRowIDs, function (index, value) {
                                //get order_no
                                var ordnm = $("#listOrder").jqGrid('getCell', value, 'order_no');

                                //add to array
                                ordMultiSel.push(ordnm);
                            });

                            //send ajax request
                            $.ajax({
                                url: "Activity.aspx/multiEditOrdGrid",
                                dataType: "json",
                                type: 'POST',
                                contentType: 'application/json; charset=utf-8',
                                data: JSON.stringify({ oper: 'delete', orders: ordMultiSel }),
                                success: function (data, textStatus, jqXHR) {
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

                                    return true;
                                },
                                error: function (jqXHR, status, error) {
                                    //Show ajax errors in dialog
                                    ErrorHandling(jqXHR, "delete orders");
                                    return false;
                                }
                            });

                            //reload grid
                            jQuery('#listOrder').trigger("reloadGrid");
                        }

                        //Clear message
                        $('#confirmDeleteMsg').empty();

                        $(this).dialog("close");
                    },
                    "Cancel": function () {
                        //Clear message
                        $('#confirmDeleteMsg').empty();

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
                        $('#amRemarkSave').click();

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

        /* Requirements Grid */
        var reqMultiSelChanged = false;
        $(function () {
            //Create jqGrid, set options
            $("#listReq").jqGrid({
                url: "Activity.aspx/getReqGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { AssetManager: "", Part_no: "", ViewHistory: "" }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //Get asset manager
                    postData.AssetManager = jQuery('#reqButton1').prop('value');

                    //Get Part
                    postData.Part_no = jQuery('#reqButton2').prop('value');

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
                    var mygrid = jQuery("#listReq")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listReq")[0];
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

                    ErrorHandling(xhr, "requirement grid");
                },
                gridComplete: function () {
                    var ids = jQuery("#listReq").jqGrid('getDataIDs');
                    for (var i = 0; i < ids.length; i++) {
                        btnGoToOrder = "<button id='viewOrder_" + ids[i] + "' type='button' class='gridButton' title='View Orders' value='" + ids[i] + "'></button>";

                        //Add button to jqGrid
                        jQuery("#listReq").jqGrid('setRowData', ids[i], { actions: btnGoToOrder });

                        //Goto Order Button
                        $('#' + ids[i] + ' :button[id^="viewOrder_"]').button({
                            icons: {
                                primary: "ui-icon-arrowrefresh-1-s"
                            },
                            text: false
                        })
                        .click(function () {
                            //Remove button highlight
                            $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                            
                            //Get button value
                            var buttonValue = $(this).prop('value');
                            var selectedReqRows = jQuery('#listReq').jqGrid('getGridParam', 'selarrrow');

                            for (var i = 0; i < selectedReqRows.length; i++) {
                                if (selectedReqRows[i] == buttonValue) {
                                    jQuery('#listReq').jqGrid('setSelection', buttonValue, false);
                                }
                            }

                            //Reload order grid based on requirement selection, goto order tab
                            jQuery('#listReq').jqGrid('setSelection', buttonValue);
                            jQuery('#tabs').tabs('option', 'active', 1);

                            //Prevent default action after close, no postback
                            return false;
                        });
                    }
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                //pager: '#pagerReq', //Show bottom paging and nav bar
                rowNum: 999999, //Default number of rows to display
                //rowList: [10, 20, 30], //User options for number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: true, //Show total records in pager
                sortable: true, //Allow sorting
                sortname: 'spo_export_date', //Default sort field
                sortorder: 'desc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                multiselect: true,
                caption: '',
                colModel: [
                        { name: 'actions', label: '-', sortable: false, align: 'center' },
                        { name: 'internal_order_no', sorttype: 'int', sortable: false, hidden: true },
                        { name: 'program_code', label: 'Program', sorttype: 'text', align: 'center' },
                        { name: 'part_no', label: 'Part #', sorttype: 'text', align: 'left', width: 180 },
                        { name: 'description', label: 'Part Name', sortable: false, align: 'left', width: 220 },
                        { name: 'status', label: 'Overall Status', sorttype: 'text', align: 'left', width: 330 },
                        { name: 'change_reason', label: 'Change Reason', sorttype: 'text', align: 'left', width: 220 },
                        { name: 'item_cost', label: 'Cost', sorttype: 'currency', align: 'left',
                            formatter: 'currency',
                            formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                        },
                        { name: 'request_due_date', label: 'Due Date', sorttype: 'date', datefmt: 'm/d/Y', align: 'left' },
                        { name: 'spo_export_date', label: 'Export Date', sorttype: 'date', datefmt: 'm/d/Y', align: 'left' },
                        { name: 'spo_export_user', label: 'SPO User', sorttype: 'text', align: 'left' },
                        { name: 'spo_request_id', label: 'Request ID', sorttype: 'int', align: 'left', hidden: true },
                       // PRP 04/22/16: added for testing new fields requested
                        // MIN_BUY_QTY
                        // SUPPLIER_MONTHLY_CAPACITY_QTY
                        // ANNUAL_BUY_IND
                        { name: 'order_quantity', label: 'Order Qty', sorttype: 'text', align: 'left', width: 50 },
                        { name: 'min_buy_qty', label: 'Min Buy', sorttype: 'text', align: 'left', width: 50 },
                        { name: 'total_monthly_capacity_qty', label: 'Mo Capacity', sorttype: 'text', align: 'left', width: 50 },
                        { name: 'annual_buy_ind', label: 'Annual Buy Ind', sorttype: 'text', align: 'left', width: 50 }
                       ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "1", //set grid row id by column model zero based index
                    repeatitems: false
                },
                ondblClickRow: function (rowid, iRow, iCol) {
                    var $reqGrid = jQuery('#listReq');
                    var selectedReqRows = $reqGrid.jqGrid('getGridParam', 'selarrrow');

                    for (var i = 0; i < selectedReqRows.length; i++) {
                        if (selectedReqRows[i] == rowid) {
                            $reqGrid.jqGrid('setSelection', rowid, false);
                        }
                    }

                    $reqGrid.jqGrid('setSelection', rowid);

                    $("#tabs").tabs('option', 'active', 1);
                },
                onSelectRow: function (rowid, status) {
                    reqMultiSelChanged = true;
                },
                onSelectAll: function (rowid, status) {
                    reqMultiSelChanged = true;
                }
            });

            //Add Navigation Bar
            $("#listReq").jqGrid('navGrid', '#pagerReq',
            { cloneToTop: true,
                edit: false,
                add: false,
                del: false,
                search: false,
                refresh: true
            }).navButtonAdd('#listReq_toppager_left', {
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
                    jQuery("#listReq").trigger("reloadGrid");
                },
                position: "last"
            });
        });

        /* Orders Grid */
        var ordMultiSel; //selected orders
        var ordJustificationIds;  // list internal order numbers requiring change justification
        var ordValidation = new Array(); //order validation
        $(function () {
            //Create jqGrid, set options
            $("#listOrder").jqGrid({
                url: "Activity.aspx/getOrdGrid",
                editurl: "Activity.aspx/editOrdGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { OrderNumKey: {} }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //get all selected row ids
                    var selectedReqRows = jQuery('#listReq').jqGrid('getGridParam', 'selarrrow');

                    if (selectedReqRows) {
                        postData.OrderNumKey = selectedReqRows;
                    }
                    else {
                        postData.OrderNumKey = {};
                    }

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
                    var mygrid = jQuery("#listOrder")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listOrder")[0];
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

                    ErrorHandling(xhr, "orders grid");
                },
                gridComplete: function () {
                    //Variables
                    var $reqGrid = jQuery('#listReq');
                    var $ordGrid = jQuery('#listOrder');
                    var ord_ids = $ordGrid.jqGrid('getDataIDs');

                    for (var i = 0; i < ord_ids.length; i++) {
                        btnCancel = "<button id='cancel_" + ord_ids[i] + "' type='button' class='gridButton' title='Cancel' value='" + ord_ids[i] + "'></button>";
                        btnSave = "<button id='save_" + ord_ids[i] + "' type='button' class='gridButton' title='Save' value='" + ord_ids[i] + "'></button>";
                        btnEdit = "<button id='edit_" + ord_ids[i] + "' type='button' class='gridButton' title='Edit' value='" + ord_ids[i] + "'></button>";

                        var ordRowData = $ordGrid.getRowData(ord_ids[i]);
                        btnComments = "<button id='comment_" + ordRowData.order_no + "' type='button' class='gridButton' title='View Comment' value='" + ordRowData.order_no + "'></button>";

                        //Disable and hide LTA button if no LTA
                        if (ordRowData.pricebreak == 'false') {
                            //Add button to jqGrid
                            $ordGrid.jqGrid('setRowData', ord_ids[i], { actions: btnEdit + btnCancel + "  " + btnSave, comments: btnComments });
                        }
                        else {
                            var reqRowData = $reqGrid.getRowData(ordRowData.internal_order_no);
                            btnLta = "<button id='lta_" + reqRowData.part_no + "' type='button' class='gridButton' title='View LTA' value='" + reqRowData.part_no + "'>" + ordRowData.pricePoint + "</button>";

                            //Add button to jqGrid
                            $ordGrid.jqGrid('setRowData', ord_ids[i], { actions: btnEdit + btnCancel + "  " + btnSave, lta: btnLta, comments: btnComments });

                            /* create jQuery object in DOM */
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
                            $("#tabs").tabs('option', 'active', 2);

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

                            //Row must be save first if new record
                            if (remarkGridID == "") {
                                //Show message
                                showInfoDialog("New orders must be saved before adding a comment.");
                            }
                            else {
                                //Open Dialog
                                $("#lookup-comments").dialog("open");

                                //Update grid size and reload grids
                                $("#listAmRemark").setGridWidth($("#amRemarkGridWrapper").width());
                                $("#listAmRemark").setGridHeight(100);
                                jQuery("#listAmRemark").trigger("reloadGrid");

                                $("#listCcbRemark").setGridWidth($("#ccbRemarkGridWrapper").width());
                                $("#listCcbRemark").setGridHeight(100);
                                jQuery("#listCcbRemark").trigger("reloadGrid");
                            }

                            //Prevent default action after close, no postback
                            return false;
                        });

                        //Edit Button
                        $('#' + ord_ids[i] + ' :button[id^="edit_"]').button({
                            icons: {
                                primary: "ui-icon-pencil"
                            },
                            text: false
                        })
                        .click(function () {
                            //Remove button highlight
                            $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                            
                            //Get button value
                            var buttonValue = $(this).prop('value');

                            //Set selection variables
                            var ordGrid = jQuery('#listOrder');

                            //Reset selections, set current
                            ordGrid.jqGrid('resetSelection');
                            ordGrid.jqGrid('setSelection', buttonValue);

                            //Edit Row
                            ordGrid.jqGrid('editRow', buttonValue, true);

                            //Show buttons
                            $(this).hide();
                            jQuery('#' + buttonValue + ' :button[id^="cancel_"]').show();
                            jQuery('#' + buttonValue + ' :button[id^="save_"]').show();
                            jQuery('#groupOrderGrid').prop("disabled", true).addClass("ui-state-disabled");
                            jQuery('#addOrderToGrid').prop("disabled", true).addClass("ui-state-disabled");
                            jQuery('#deleteOrderFromGrid').prop("disabled", true).addClass("ui-state-disabled");
                            jQuery('#bulkValidate').prop("disabled", true).addClass("ui-state-disabled");
                            jQuery('#bulkApprove').prop("disabled", true).addClass("ui-state-disabled");
                            jQuery('#bulkReject').prop("disabled", true).addClass("ui-state-disabled");
                            jQuery('#bulkReason').prop("disabled", true).addClass("ui-state-disabled");
                            jQuery('#listOrder :button[id^="edit_"]').button('disable');

                            //Prevent default action after close, no postback
                            return false;
                        });

                        //Cancel Button
                        $('#' + ord_ids[i] + ' :button[id^="cancel_"]').button({
                            icons: {
                                primary: "ui-icon-cancel"
                            },
                            text: false
                        })
                    .click(function () {
                        //Remove button highlight
                        $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                        
                        //Get button value
                        var buttonValue = $(this).prop('value');
                        var $ordGrid = jQuery('#listOrder');

                        //Restore last row, reset
                        if (buttonValue == "new_row") {
                            $ordGrid.trigger('reloadGrid');
                        }
                        else {
                            $ordGrid.jqGrid('restoreRow', buttonValue).jqGrid('resetSelection');
                        }

                        //Show buttons
                        $(this).hide();
                        jQuery('#' + buttonValue + ' :button[id^="save_"]').hide();
                        jQuery('#' + buttonValue + ' :button[id^="edit_"]').show();
                        jQuery('#groupOrderGrid').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#addOrderToGrid').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#deleteOrderFromGrid').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#bulkValidate').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#bulkApprove').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#bulkReject').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#bulkReason').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#listOrder :button[id^="edit_"]').button('enable');

                        //Prevent default action after close, no postback
                        return false;
                    }).hide();

                        //Save Button
                        $('#' + ord_ids[i] + ' :button[id^="save_"]').button({
                            icons: {
                                primary: "ui-icon-disk"
                            },
                            text: false
                        })
                    .click(function () {
                        //Remove button highlight
                        $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                        
                        //Get button value
                        var buttonValue = $(this).prop('value');
                        var $ordGrid = jQuery('#listOrder');

                        //Save, check response for data save error
                        $ordGrid.jqGrid('saveRow', buttonValue,
                                                function (data) {
                                                    var response = $.parseJSON(data.responseText);

                                                    //Show errors
                                                    if (response.d.hasError) {
                                                        var ers = "";

                                                        //prepare each error in array for display
                                                        $.each(response.d.errorList, function (index, value) {
                                                            ers += value + '<br />';
                                                        });

                                                        //Show info dialog
                                                        showErrorDialog(ers);
                                                    }

                                                    //Show messages
                                                    if (response.d.hasMsg) {
                                                        var msg = "";

                                                        //prepare each msg in array for display
                                                        $.each(response.d.msgList, function (index, value) {
                                                            msg += value + '<br />';
                                                        });

                                                        //Show info dialog
                                                        showInfoDialog(msg);
                                                    }

                                                    //Process .net return data
                                                    ordValidation = new Array();
                                                    if (response.d.hasReturnData) {
                                                        //save data to global variable / process after grid reload
                                                        $.each(response.d.returnData, function (index, value) {
                                                            ordValidation.push({ order: index, result: value });
                                                        });
                                                    }

                                                    //Cycle through callbacks and respond appropriately
                                                    // 0 = callback for Variance Comment dialog
                                                    ordJustificationIds = new Array();
                                                    if (response.d.hasCallback) {
                                                        $.each(response.d.callbackList, function (index, value) {
                                                            if (index == 0) {
                                                                $.each(value, function (key, data) {
                                                                    if (data == 'true') {
                                                                        ordJustificationIds.push(key);
                                                                    }
                                                                    var $varGrid = $("#listVariance");

                                                                    //Open Dialog
                                                                    $("#lookup-variance").dialog("open");

                                                                    //Update grid size
                                                                    $varGrid.setGridWidth($("#varianceGridWrapper").width());
                                                                    $varGrid.setGridHeight(300);
                                                                })
                                                            };
                                                        })
                                                    }

                                                    return true;
                                                }
                        );

                        //Reload grid
                        $ordGrid.trigger('reloadGrid');

                        //Show buttons
                        $(this).hide();
                        jQuery('#' + buttonValue + ' :button[id^="cancel_"]').hide();
                        jQuery('#' + buttonValue + ' :button[id^="edit_"]').show();
                        jQuery('#groupOrderGrid').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#addOrderToGrid').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#deleteOrderFromGrid').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#bulkValidate').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#bulkApprove').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#bulkReject').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#bulkReason').prop("disabled", false).removeClass("ui-state-disabled");
                        jQuery('#listOrder :button[id^="edit_"]').button('enable');

                        //Prevent default action after close, no postback
                        return false;
                    }).hide();
                    }

                    if (ordValidation.length > 0) {
                        //Process color coding
                        $.each(ord_ids, function (r_index, r_value) {
                            //hold number of validations processed
                            var count = 0;

                            //get order_no
                            var current_ord_num = $ordGrid.jqGrid('getCell', r_value, 'order_no');

                            //Check if found in array
                            $.each(ordValidation, function (v_index, v_value) {
                                if (v_value.order == current_ord_num) {
                                    //set order row highlight
                                    if (v_value.result == 'true') {
                                        jQuery('#' + r_value + ' td[aria-describedby="listOrder_cost_charge_number"]').removeClass("gridCcnInValid");
                                        jQuery('#' + r_value + ' td[aria-describedby="listOrder_cost_charge_number"]').addClass("gridCcnValid");
                                    }
                                    if (v_value.result == 'false') {
                                        jQuery('#' + r_value + ' td[aria-describedby="listOrder_cost_charge_number"]').removeClass("gridCcnValid");
                                        jQuery('#' + r_value + ' td[aria-describedby="listOrder_cost_charge_number"]').addClass("gridCcnInValid");
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
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                //pager: '#pagerOrd', //Show bottom paging and nav bar
                rowNum: 999999, //Default number of rows to display
                //rowList: [10, 20, 30], //User options for number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
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
                            { name: 'actions', label: '-', sortable: false, align: 'center' },
                            { name: 'id', key: true, hidden: true },
                            { name: 'internal_order_no', editable: false, editrules: { edithidden: true }, hidden: true },
                            { name: 'requirement_schedule_no', hidden: true },
                            { name: 'spo_request_id', editable: false, hidden: true },
                            { name: 'part_no', label: 'Part #', sorttype: 'text', align: 'left', editable: false },
                            { name: 'order_no', label: 'Order No', sorttype: 'text', align: 'center', width: 180 },
                            { name: 'due_date', label: 'Due Date', sorttype: 'date', datefmt: 'm/d/Y', align: 'center', editable: true,
                                editoptions: { maxlength: 10, dataInit: function (el) { $(el).datepicker({ dateFormat: 'm/d/yy' }); },
                                    defaultValue: '<%= DateTime.Today.Month + "/" + DateTime.Today.Day + "/" + DateTime.Today.Year %>'
                                },
                                editrules: { required: true, date: true }
                            },
                            { name: 'order_quantity', label: 'Order Qty', sorttype: 'int', align: 'center', editable: true, editrules: { required: true, integer: true, minValue: 1} },
                            { name: 'priority', label: 'Priority', sorttype: 'int', align: 'center', editable: true, edittype: 'select',
                                editoptions: { value: "1:1;2:2;3:3", defaultValue: "3" }, editrules: { required: true, integer: true }
                            },
                            { name: 'cost_charge_number', label: 'CCN', sorttype: 'text', align: 'center', editable: true },
                            { name: 'change_reason', label: 'Change Reason', sorttype: 'text', width: 360, align: 'center',
                                editable: true, edittype: 'select', editrules: { required: false },
                                editoptions: { value: '<%= getChangeReasons() %>' }
                            },
                            { name: 'activity_status', label: 'Activity Status', sorttype: 'text', width: 360, align: 'center',
                                editable: true, edittype: 'select', editrules: { required: true },
                                editoptions: { value: '20:Awaiting Asset Manager Review;30:Approved by Asset Manager;40:Rejected by Asset Manager' }
                            },
                            { name: 'spo_qty', label: 'SPO Qty', sorttype: 'int', align: 'center' },
                            { name: 'order_total', label: 'Order Total', sorttype: 'int', align: 'center' },
                            { name: 'lta', label: 'LTA', sortable: false, align: 'center' },
                            { name: 'pricebreak', sortable: false, editable: false, hidden: true },
                            { name: 'pricePoint', sortable: false, editable: false, hidden: true, formatter: 'currency',
                                formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2 }
                            },
                            { name: 'comments', label: 'Comments', sortable: false, align: 'center' }
                           ],
                jsonReader: {
                    root: "rows",
                    page: "currentPage",
                    total: "totalPages",
                    records: "totalRows",
                    id: "id", //set grid row id by column model zero based index or name
                    repeatitems: false
                },
                grouping: true,
                groupingView: {
                    groupField: ['part_no'],
                    groupColumnShow: [false],
                    groupText: ['Part: <b>{0}</b>']
                },
                ondblClickRow: function (rowid, iRow, iCol) {
                    $('#' + rowid + ' :button[id^="edit_"]').click();
                },
                ajaxRowOptions: {
                    contentType: "application/json; charset=utf-8"
                },
                serializeRowData: function (postData) {
                    //Update params if delete
                    if (postData.delFlag) {
                        //Change operation
                        postData.oper = "delete";

                        //Clear flag
                        postData.delFlag = null;
                    }

                    //Update params if new
                    if (postData.id == "new_row") {
                        //Change operation
                        postData.oper = "new";

                        //Add valid values
                        postData.id = "0";
                        postData.requirement_schedule_no = "0";

                        //Get key from selected requirements grid
                        var sel_req_id = jQuery('#listSelectedReq').jqGrid('getGridParam', 'selrow');
                        postData.internal_order_no = parseInt(sel_req_id);
                    }

                    //add requirement_schedule_no for non-new records
                    if (postData.oper != "new") {
                        var rowdata = jQuery('#listOrder').getRowData(postData.id);
                        postData.requirement_schedule_no = rowdata.requirement_schedule_no;

                        postData.internal_order_no = rowdata.internal_order_no;
                    }

                    //set correct datatype for other post parameters
                    postData.id = parseInt(postData.id);
                    postData.internal_order_no = parseInt(postData.internal_order_no);
                    postData.order_quantity = parseInt(postData.order_quantity);
                    postData.priority = parseInt(postData.priority);
                    postData.requirement_schedule_no = parseInt(postData.requirement_schedule_no);
                    postData.activity_status = parseInt(postData.activity_status);

                    //Convert JSON to JSONstring
                    var postDataString = JSON.stringify(postData);
                    return postDataString;
                }
            });

            //Add Navigation Bar
            jQuery("#listOrder").jqGrid('navGrid', '#pagerOrd',
            {/*navGrid options*/
                cloneToTop: true,
                edit: false,
                add: false,
                del: false,
                search: false,
                refresh: true
            }
        ).navButtonAdd('#listOrder_toppager_left', {
            id: "groupOrderGrid",
            caption: "",
            title: "Group / Ungroup Orders",
            buttonicon: "ui-icon-shuffle",
            onClickButton: function () {
                var $ordGrid = jQuery('#listOrder');

                //Check grid grouping current state
                var currentlyGrouped = $ordGrid.jqGrid('getGridParam', 'grouping');

                if (currentlyGrouped) {
                    //Remove grouping
                    $ordGrid.jqGrid('groupingRemove', true);
                }
                else {
                    //Add grouping
                    $ordGrid.jqGrid('groupingGroupBy', 'part_no');
                }

                //Prevent default action of following link
                return false;
            },
            position: "last"
        }).navButtonAdd('#listOrder_toppager_left', {
            id: "addOrderToGrid",
            caption: "",
            title: "Split",
            buttonicon: "ui-icon-copy",
            onClickButton: function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");

                //Variables
                var $reqGrid = jQuery('#listReq');
                var $ordGrid = jQuery('#listOrder');
                var $selectedReqGrid = jQuery('#listSelectedReq');

                //Get all selected requirements
                var selectedReqRowIDs = $reqGrid.jqGrid('getGridParam', 'selarrrow');

                //Continue if at least 1 requirement is selected
                if (selectedReqRowIDs.length > 0) {
                    //Clear data from grid
                    $selectedReqGrid.jqGrid('clearGridData');

                    //Populate grid with array data
                    $.each(selectedReqRowIDs, function (index, value) {
                        //get data
                        var reqSelRowData = $reqGrid.getRowData(value);

                        //add to grid
                        $selectedReqGrid.jqGrid('addRowData', value,
                            { program_code: reqSelRowData.program_code,
                                part_no: reqSelRowData.part_no,
                                spo_export_date: reqSelRowData.spo_export_date
                            });
                    });

                    //set height before open
                    $selectedReqGrid.setGridHeight(300);

                    //Open Dialog
                    jQuery("#lookup-selectedReq").dialog("open");

                    //Update grid size
                    $selectedReqGrid.setGridWidth($("#selectedReqGridWrapper").width());
                }
                else {
                    //Show message
                    showInfoDialog("Please select 1 or more requirements.");
                }

                //no postback
                return false;
            },
            position: "last"
        }).navButtonAdd('#listOrder_toppager_left', {
            id: "deleteOrderFromGrid",
            caption: "",
            title: "Delete",
            buttonicon: "ui-icon-trash",
            onClickButton: function () {
                //Variables
                var $ordGrid = jQuery('#listOrder');

                //Get all selected orders
                var selectedOrdRowIDs = $ordGrid.jqGrid('getGridParam', 'selarrrow');

                //process selections to get unique key
                if (selectedOrdRowIDs.length > 0) {
                    //Show dialog
                    $("#confirm-order-delete").dialog("open");
                }
                else {
                    //Show error dialog, nothing selected
                    showInfoDialog('Please select at an order.');
                }

                //Prevent default action of following link
                return false;
            },
            position: "last"
        }).navSeparatorAdd('#listOrder_toppager_left', {
            sepclass: "",
            sepcontent: "|"
        }).navButtonAdd('#listOrder_toppager_left', {
            id: "bulkValidate",
            caption: "Validate",
            title: "Validate CCN(s)",
            buttonicon: "ui-icon-arrowrefresh-1-e",
            onClickButton: function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");

                //Variables
                var $ordGrid = jQuery('#listOrder');

                //get all selected row ids
                var selectedOrderRows = $ordGrid.jqGrid('getGridParam', 'selarrrow');

                //Check number of rows in grid
                var rowCount = $ordGrid.getGridParam("reccount");

                //If only one row exists, auto-select row for user
                if (rowCount == 1) {
                    //get first row id
                    var firstRowId = $ordGrid.jqGrid('getDataIDs')[0];

                    //select row if not already selected
                    if (selectedOrderRows[0] != firstRowId) {
                        $ordGrid.jqGrid('setSelection', firstRowId);
                    }
                }

                //process selections to get unique key
                if (selectedOrderRows.length > 0) {
                    //instantiate variable to hold row keys
                    ordMultiSel = new Array();

                    //get the key for each row selected
                    $.each(selectedOrderRows, function (index, value) {
                        //get order_no
                        var ordnm = $ordGrid.jqGrid('getCell', value, 'order_no');

                        //add to array
                        ordMultiSel.push(ordnm);
                    });

                    //Open processing dialog
                    setTimeout(function () {
                        $("#wait-dialog").dialog("open");
                    }, 0);

                    //send ajax request
                    $.ajax({
                        url: "Activity.aspx/multiEditOrdGrid",
                        dataType: "json",
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        data: JSON.stringify({ oper: 'validate', orders: ordMultiSel }),
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
                            if (data.d.hasReturnData) {
                                $.each(selectedOrderRows, function (index, value) {
                                    //get order_no
                                    var ordnm = $ordGrid.jqGrid('getCell', value, 'order_no');

                                    //set order row highlight
                                    if (data.d.returnData[ordnm] == 'true') {
                                        jQuery('#' + value + ' td[aria-describedby="listOrder_cost_charge_number"]').removeClass("gridCcnInValid");
                                        jQuery('#' + value + ' td[aria-describedby="listOrder_cost_charge_number"]').addClass("gridCcnValid");
                                    }
                                    if (data.d.returnData[ordnm] == 'false') {
                                        jQuery('#' + value + ' td[aria-describedby="listOrder_cost_charge_number"]').removeClass("gridCcnValid");
                                        jQuery('#' + value + ' td[aria-describedby="listOrder_cost_charge_number"]').addClass("gridCcnInValid");
                                    }
                                });
                            }

                            return true;
                        },
                        error: function (jqXHR, status, error) {
                            //Close processing dialog
                            setTimeout(function () {
                                $("#wait-dialog").dialog("close");
                            }, 0);

                            //Show error dialog
                            ErrorHandling(jqXHR, "multi edit order ");
                            return false;
                        }
                    });
                }
                else {
                    //Show error dialog
                    showErrorDialog('Please select an order.');
                }

                //clear variable
                ordMultiSel = null;

                //Prevent default action after close, no postback
                return false;
            },
            position: "last"
        }).navSeparatorAdd('#listOrder_toppager_left', {
            sepclass: "",
            sepcontent: " "
        }).navButtonAdd('#listOrder_toppager_left', {
            id: "bulkApprove",
            caption: "Approve",
            title: "Approve Order(s)",
            buttonicon: "ui-icon-check",
            onClickButton: function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");

                //Variables
                var $ordGrid = jQuery('#listOrder');

                //get all selected row ids
                var selectedOrderRows = $ordGrid.jqGrid('getGridParam', 'selarrrow');

                //Check number of rows in grid
                var rowCount = $ordGrid.getGridParam("reccount");

                //If only one row exists, auto-select row for user
                if (rowCount == 1) {
                    //get first row id
                    var firstRowId = $ordGrid.jqGrid('getDataIDs')[0];

                    //select row if not already selected
                    if (selectedOrderRows[0] != firstRowId) {
                        $ordGrid.jqGrid('setSelection', firstRowId);
                    }
                }

                //process selections to get unique key
                if (selectedOrderRows.length > 0) {
                    //instantiate variable to hold row keys
                    ordMultiSel = new Array();

                    //get the key for each row selected
                    $.each(selectedOrderRows, function (index, value) {
                        //get order_no
                        var ordnm = $ordGrid.jqGrid('getCell', value, 'order_no');

                        //add to array
                        ordMultiSel.push(ordnm);
                    });

                    //Open processing dialog
                    setTimeout(function () {
                        $("#wait-dialog").dialog("open");
                    }, 0);

                    //send ajax request
                    $.ajax({
                        url: "Activity.aspx/multiEditOrdGrid",
                        dataType: "json",
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        data: JSON.stringify({ oper: 'approve', orders: ordMultiSel }),
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
                            ErrorHandling(jqXHR, 'multiedit order');

                            return false;
                        }
                    });
                }
                else {
                    //Show error dialog
                    showErrorDialog('Please select an order.');
                }

                //clear variable
                ordMultiSel = null;

                //Reload grid
                $ordGrid.trigger("reloadGrid");

                //Prevent default action after close, no postback
                return false;
            },
            position: "last"
        }).navSeparatorAdd('#listOrder_toppager_left', {
            sepclass: "",
            sepcontent: " "
        }).navButtonAdd('#listOrder_toppager_left', {
            id: "bulkReject",
            caption: "Reject",
            title: "Reject Order(s)",
            buttonicon: "ui-icon-closethick",
            onClickButton: function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");

                //Variables
                var $ordGrid = jQuery('#listOrder');

                //get all selected row ids
                var selectedOrderRows = $ordGrid.jqGrid('getGridParam', 'selarrrow');

                //Check number of rows in grid
                var rowCount = $ordGrid.getGridParam("reccount");

                //If only one row exists, auto-select row for user
                if (rowCount == 1) {
                    //get first row id
                    var firstRowId = $ordGrid.jqGrid('getDataIDs')[0];

                    //select row if not already selected
                    if (selectedOrderRows[0] != firstRowId) {
                        $ordGrid.jqGrid('setSelection', firstRowId);
                    }
                }

                //process selections to get unique key
                if (selectedOrderRows.length > 0) {
                    //instantiate variable to hold row keys
                    ordMultiSel = new Array();

                    //get the key for each row selected
                    $.each(selectedOrderRows, function (index, value) {
                        //get order_no
                        var ordnm = $ordGrid.jqGrid('getCell', value, 'order_no');

                        //add to array
                        ordMultiSel.push(ordnm);
                    });

                    //Open processing dialog
                    setTimeout(function () {
                        $("#wait-dialog").dialog("open");
                    }, 0);

                    //send ajax request
                    $.ajax({
                        url: "Activity.aspx/multiEditOrdGrid",
                        dataType: "json",
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        data: JSON.stringify({ oper: 'reject', orders: ordMultiSel }),
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

                            return true;
                        },
                        error: function (jqXHR, status, error) {
                            //close processing dialog
                            setTimeout(function () {
                                $("#wait-dialog").dialog("close");
                            }, 0);

                            //Show error dialog
                            ErrorHandling(jqXHR, 'multiedit order');

                            return false;
                        }
                    });
                }
                else {
                    //Show error dialog
                    showErrorDialog('Please select an order.');
                }

                //clear variable
                ordMultiSel = null;

                //Reload grid
                $ordGrid.trigger("reloadGrid");

                //Prevent default action after close, no postback
                return false;
            },
            position: "last"
        }).navSeparatorAdd('#listOrder_toppager_left', {
            sepclass: "",
            sepcontent: " "
        }).navButtonAdd('#listOrder_toppager_left', {
            id: "bulkReason",
            caption: "Reason",
            title: "Add Change Reason(s)",
            buttonicon: "ui-icon-flag",
            onClickButton: function () {
                //Remove button highlight
                $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");

                //Variables
                var $ordGrid = jQuery('#listOrder');

                //get all selected row ids
                var selectedOrderRows = $ordGrid.jqGrid('getGridParam', 'selarrrow');

                //Check number of rows in grid
                var rowCount = $ordGrid.getGridParam("reccount");

                //If only one row exists, auto-select row for user
                if (rowCount == 1) {
                    //get first row id
                    var firstRowId = $ordGrid.jqGrid('getDataIDs')[0];

                    //select row if not already selected
                    if (selectedOrderRows[0] != firstRowId) {
                        $ordGrid.jqGrid('setSelection', firstRowId);
                    }
                }

                //process selections to get unique key
                if (selectedOrderRows.length > 0) {
                    //instantiate variable to hold row keys
                    ordJustificationIds = new Array();

                    //get the key for each row selected
                    $.each(selectedOrderRows, function (index, value) {
                        //get order_no
                        var ordnm = $ordGrid.jqGrid('getCell', value, 'internal_order_no');

                        //add to array
                        ordJustificationIds.push(ordnm);
                    });

                    var $varGrid = $("#listVariance");

                    //Open Dialog
                    $("#lookup-variance").dialog("open");

                    //Update grid size
                    $varGrid.setGridWidth($("#varianceGridWrapper").width());
                    $varGrid.setGridHeight(300);
                    //Open processing dialog
                }
                else {
                    //Show error dialog
                    showErrorDialog('Please select an order.');
                }

                //clear variable
                ordMultiSel = null;

                //Reload grid
                $ordGrid.trigger("reloadGrid");

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
                url: "Activity.aspx/getManGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { AssetManager: "", ViewHistory: "" }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //Get asset manager
                    postData.AssetManager = jQuery('#reqButton1').prop('value');

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

                    ErrorHandling(xhr, "asset manager grid");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                //pager: '#pagerReq', //Show bottom paging and nav bar
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
                url: "Activity.aspx/getPrtGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { Part_no: "", LtaPart: "false", ViewHistory: "" }, //Add default custom post parameters
                serializeGridData: function (postData) {
                    //get part number
                    postData.Part_no = jQuery('#reqButton2').prop('value');

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

                    ErrorHandling(xhr, "part grid");
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
                caption: '',
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
        });

        /* New Buy Variance Comment Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listVariance").jqGrid({
                url: "Activity.aspx/getVarianceCommentGrid",
                datatype: 'json',
                mtype: 'POST',
                postData: { comment: "" }, //Add default custom post parameters
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
                    var mygrid = jQuery("#listVariance")[0];
                    mygrid.addJSONData(data.d);
                },
                loadError: function (xhr, status, error) {
                    var mygrid = jQuery("#listVariance")[0];
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

                    ErrorHandling(xhr, "new buy variance comment grid");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                //pager: '#pagerReq', //Show bottom paging and nav bar
                rowNum: 999999, //Default number of rows to display
                //rowList: [10, 20, 30], //User options for number of rows to display
                pgbuttons: false, //Show paging buttons
                pginput: false, //Show paging input
                viewrecords: true, //Show total records in pager
                sortable: true, //Allow sorting
                sortname: 'comment', //Default sort field
                sortorder: 'asc', //Default sort field direction, asc/desc
                gridview: true,
                hidegrid: false,
                rownumbers: false,
                caption: '',
                colModel: [
                       { name: 'comment', label: 'Comment', sorttype: 'text', align: 'left' }
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

            //Add Navigation Bar
            $("#listVariance").jqGrid('navGrid', '#pagerVariance',
                    { cloneToTop: true,
                        edit: false,
                        add: false,
                        del: false,
                        search: false,
                        refresh: true
                    }
                );
        });

        /* AM Remark Grid */
        var remarkRowID;
        var remarkSaved;
        $(function () {
            //Create jqGrid, set options
            $("#listAmRemark").jqGrid({
                url: "Activity.aspx/getAmRemarkGrid",
                editurl: "Activity.aspx/editAmRemark",
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

                    ErrorHandling(xhr, "asset managers remarks grid");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: true, //Show top paging and nav bar
                //pager: '#pagerAmRemark', //Show bottom paging and nav bar
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
                    var remark_ids = jQuery("#listAmRemark").jqGrid('getDataIDs');

                    //If data exists
                    if (remark_ids[0]) {
                        //select first row, call onselectrow
                        jQuery("#listAmRemark").setSelection(remark_ids[0], true);
                    }
                },
                onSelectRow: function (rowid, status) {
                    jQuery('#listAmRemark').jqGrid('editRow', rowid, true);

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
            $("#listAmRemark").jqGrid('navGrid', '#pagerAmRemark',
            { cloneToTop: true,
                edit: false,
                add: false,
                del: false,
                search: false,
                refresh: true
            }
        ).navButtonAdd('#listAmRemark_toppager_left', {
            id: "amRemarkSave",
            caption: "",
            title: "Save",
            buttonicon: "ui-icon-disk",
            onClickButton: function () {
                //Save row when a row selected
                if (remarkRowID) {
                    var rData = $("#" + remarkRowID + "_asset_manager_remark").prop('value');
                    var rValidationResults = validateAMRemark(rData);

                    //save row when it contains valid input
                    if (!rValidationResults[0]) {
                        //Show error dialog
                        showErrorDialog(rValidationResults[1]);
                    }
                    else {
                        //Save, check response for data save error
                        $('#listAmRemark').jqGrid('saveRow', remarkRowID,
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

                        jQuery('#listAmRemark').trigger('reloadGrid');
                    }
                }

                //Prevent default action of following link
                return false;
            },
            position: "last"
        });

            //Hide Grid column headers
            $("#gview_listAmRemark > .ui-jqgrid-hdiv").hide();
        });

        /* CCB Remark Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listCcbRemark").jqGrid({
                url: "Activity.aspx/getCcbRemarkGrid",
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
                    ErrorHandling(xhr, "ccb remark grid");
                },
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: false, //Show top paging and nav bar
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
            $("#gview_listCcbRemark > .ui-jqgrid-hdiv").hide();
        });

        /* Selected Requirements Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listSelectedReq").jqGrid({
                datatype: 'local',
                loadui: 'block', //Loading message options
                autowidth: true,
                height: '100%',
                toppager: false, //Show top paging and nav bar
                //pager: '#pagerSelectedReq', //Show bottom paging and nav bar
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
                caption: '',
                colModel: [
                        { name: 'program_code', label: 'Program', sorttype: 'text', align: 'center' },
                        { name: 'part_no', label: 'Part #', sorttype: 'text', align: 'left' },
                        { name: 'spo_export_date', label: 'Export Date', sorttype: 'date', datefmt: 'm/d/Y', align: 'right',
                            cellattr: function (rowId, tv, rawObject, cm, rdata) { return 'style="padding-right: 20px;"' }
                        }
                      ]
            });
        });

        /* LTA Part Grid */
        $(function () {
            //Create jqGrid, set options
            $("#listLta").jqGrid({
                url: "Activity.aspx/getLtaGrid",
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
                var grid0 = $("#listReq");
                grid0.setGridWidth($("#reqGridWrapper").width());
            }
            else if (currentTab == 1) {
                var grid1 = $("#listOrder");
                grid1.setGridWidth($("#orderGridWrapper").width());
            }
            else if (currentTab == 2) {
                var grid2 = $("#listLta");
                grid2.setGridWidth($("#ltaGridWrapper").width());
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
                url: "Activity.aspx/getLtaPartInfo",
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
        //validate am remark
        function validateAMRemark(value) {
            if (value.length > 200) {
                return [false, "Max remark length is 200 characters."];
            }

            if (value) {
                var amRemarkRegex = /[<>'"]/;
                if (amRemarkRegex.test(value)) {
                    return [false, "The following characters are not allowed: <, >, single and double quotes."];
                }
            }

            return [true, ""];
        };
</script>
</asp:Content>
