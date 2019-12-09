<%@ Page Title="Seg Code Maintenance" Language="C#" MasterPageFile="~/NewBuy.Master" AutoEventWireup="true" CodeBehind="Maintain.aspx.cs" Inherits="NewBuy.SEGCODE.Maintain" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div id="tabs">
    <ul>
        <li></li>
        <li></li>
        <li><a href="#tabs-Segcode">Segcode</a></li>
    </ul>
    <div id="tabs-Segcode">
        <div class="toolbar">
            <div id="segSrcField" class="toolbarButtonSet">
                <input type="radio" id="segSrcField1" name="radio" value="seg_code" checked="checked" /><label for="segSrcField1">Seg</label>
                <input type="radio" id="segSrcField2" name="radio" value="site_location" /><label for="segSrcField2">Site</label>
            </div>
            <input id="segSrcText" />
            <button id="segButton2" title="Search" value="">Search</button>
            <button id="segButton1" title="Clear Search" value="">Clear</button>
        </div>
        <div id="segcodeGridWrapper" class="ui-jqgrid gridWrapper">
            <table id="listSegcode"></table>
            <div id="pagerSegcode"></div>
        </div>
    </div>
</div>
<div id="confirm-segcode-delete" title="Delete Segcode?">
    <p><span class="ui-icon ui-icon-alert dialogIcon"></span>This segcode will be permanently deleted and cannot be recovered. Are you sure?</p>
</div>
<div id="error-dialog" title="Error">
    <p><span class="ui-icon ui-icon-alert dialogIcon"></span><span id="errorMsg"></span></p>
</div>
<div id="info-dialog" title="Info">
    <p><span class="ui-icon ui-icon-info dialogIcon"></span><span id="infoMsg"></span></p>
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
        //Clear Search
        $("#segButton2").button({
            icons: {
                primary: "ui-icon-search"
            },
            text: false
        })
        .click(function () {
            //Remove button highlight
            $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
            
            //reload grid
            jQuery('#listSegcode').trigger("reloadGrid");

            //Prevent default action after close, no postback
            return false;
        });
        
        //Clear Search
        $("#segButton1").button({
            icons: {
                primary: "ui-icon-cancel"
            },
            text: false
        })
        .click(function () {
            //Remove button highlight
            $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
            
            //Clear all search fields
            $('#segSrcText').prop('value', '');

            //reload grid
            jQuery('#listSegcode').trigger("reloadGrid");

            //Prevent default action after close, no postback
            return false;
        });

        //Search field buttonset
        $("#segSrcField").buttonset();

        //Autocomplete
        $("#segSrcText").autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: "Maintain.aspx/getSearchList",
                    dataType: "json",
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({ term: request.term,
                        field: $("#segSrcField :radio:checked").prop('value')
                    }),
                    success: function (data, textStatus, jqXHR) {
                        response(data.d.listValues);
                    },
                    error: function (jqXHR, status, error) {
                                  //Show error dialog
                                  ErrorHandling(jqXHR, "autocomplete ");
                                return false;
                            }
                });
            },
            minLength: 2
        }).keypress(function (e) {
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 13) { //Enter key
                //reload grid
                jQuery('#listSegcode').trigger("reloadGrid");

                //prevent postback
                return false;
            }
        });

        //Dialogs
        $("#confirm-segcode-delete").dialog({
            autoOpen: false,
            dialogClass: 'confirm',
            modal: true,
            buttons: {
                "Confirm Delete": function () {
                    //Get selected row id
                    var seg_sel_id = $('#listSegcode').jqGrid('getGridParam', 'selrow');

                    //trigger row save, add extra param and check for data errors
                    jQuery('#listSegcode').jqGrid('saveRow', seg_sel_id,
                                {
                                    extraparam: { delFlag: true },
                                    successfunc: function (data) {
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

                                        return true;
                                    }
                                }
                            );
                            lastsel = null;

                    //reload grid
                    jQuery('#listSegcode').trigger("reloadGrid");

                    $(this).dialog("close");
                },
                "Cancel": function () {
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
    });

/* Segcode Grid */
var lastsel;
$(function () {
    //Create jqGrid, set options
    $("#listSegcode").jqGrid({
        url: "Maintain.aspx/getSegcodeGrid",
        editurl: "Maintain.aspx/editSegcodeGrid",
        datatype: 'json',
        mtype: 'POST',
        postData: { FilterField: "seg_code", FilterValue: "" }, //Add default custom post parameters
        serializeGridData: function (postData) {
            //Get value from autocomplete search
            postData.FilterValue = $("#segSrcText").prop('value');
            postData.FilterField = $("#segSrcField :radio:checked").prop('value');

            //Check for nulls
            if (!postData.FilterValue) postData.FilterValue = "";
            if (!postData.FilterField) postData.FilterField = "";

            //Convert datatype
            if (postData.Page) postData.Page = parseInt(postData.Page);
            if (postData.RowLimit) postData.RowLimit = parseInt(postData.RowLimit);


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
            var mygrid = jQuery("#listSegcode")[0];
            mygrid.addJSONData(data.d);
        },
        loadError: function (xhr, status, error) {
            var mygrid = jQuery("#listSegcode")[0];
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
            ErrorHandling(xhr, " segcode grid ");
        },
        gridComplete: function () {
            var seg_ids = jQuery("#listSegcode").jqGrid('getDataIDs');
            for (var i = 0; i < seg_ids.length; i++) {
                btnCancel = "<button id='cancel_" + seg_ids[i] + "' type='button' class='gridButton' title='Cancel' value='" + seg_ids[i] + "'></button>";
                btnSave = "<button id='save_" + seg_ids[i] + "' type='button' class='gridButton' title='Save' value='" + seg_ids[i] + "'></button>";

                //Add button to jqGrid
                jQuery("#listSegcode").jqGrid('setRowData', seg_ids[i], { actions: btnCancel + "  " + btnSave });

                /* create jQuery button objects in DOM */
                //Cancel Button
                $('#cancel_' + seg_ids[i]).button({
                    icons: {
                        primary: "ui-icon-cancel"
                    },
                    text: false,
                    disabled: true
                })
                    .click(function () {
                        //Remove button highlight
                        $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                        
                        //Get button value
                        var buttonValue = $(this).prop('value');

                        //Restore last row, reset
                        $('#listSegcode').jqGrid('restoreRow', buttonValue);
                        $('#listSegcode').jqGrid('resetSelection');
                        lastsel = null;

                        //Disable buttons
                        $(this).button("disable");
                        jQuery('#save_' + buttonValue).button("disable");

                        //Prevent default action after close, no postback
                        return false;
                    });

                //Save Button
                $('#save_' + seg_ids[i]).button({
                    icons: {
                        primary: "ui-icon-disk"
                    },
                    text: false,
                    disabled: true
                })
                    .click(function () {
                        //Remove button highlight
                        $(this).removeClass("ui-state-active ui-state-hover ui-state-focus");
                        
                        //Get button value
                        var buttonValue = $(this).prop('value');

                        //Save, check response for data save error
                        $('#listSegcode').jqGrid('saveRow', buttonValue,
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

                                return true;
                            }
                        );

                        lastsel = null;
                        $('#listSegcode').trigger('reloadGrid');

                        //Disable buttons
                        $(this).button("disable");
                        jQuery('#cancel_' + buttonValue).button("disable");

                        //Prevent default action after close, no postback
                        return false;
                    });
            }
        },
        loadui: 'block', //Loading message options
        autowidth: true,
        height: '100%',
        toppager: true, //Show top paging and nav bar
        pager: '#pagerSegcode', //Show bottom paging and nav bar
        rowNum: 20, //Default number of rows to display
        rowList: [20, 50, 99], //user number of rows to display, max 99
        pgbuttons: true, //Show paging buttons
        pginput: true, //Show paging input
        viewrecords: true, //Show total records in pager
        sortable: true, //Allow sorting
        sortname: 'last_update_date', //Default sort field
        sortorder: 'desc', //Default sort field direction, asc/desc
        gridview: true,
        hidegrid: false,
        rownumbers: false,
        caption: '',
        colModel: [
                        { name: 'actions', label: '-', sortable: false, align: 'center' },
                        { name: 'seg_code', label: 'Segcode', sorttype: 'text', align: 'center', editable: true, editrules: { required: true }, width: 250 },
                        { name: 'program_code', label: 'Program', sorttype: 'text', align: 'center', editable: true, editrules: { required: true} },
                        { name: 'last_update_date', label: 'Updated', sorttype: 'date', datefmt: 'm/d/Y', align: 'center' },
                        { name: 'last_update_user', label: 'Updated By', sorttype: 'text', align: 'center' },
                        { name: 'buy_method', label: 'Buy Method', sorttype: 'text', align: 'center', editable: true, edittype: 'select', editoptions: { value: " : ;COC:COC;DD250:DD250"} },
                        { name: 'site_location', label: 'Site', sorttype: 'text', align: 'center', editable: true, width: 260 },
                        { name: 'include_in_tav_reporting', label: 'TAV', sorttype: 'text', align: 'center', editable: true, edittype: 'checkbox', editoptions: { value: "Y:N", required: true} },
                        { name: 'include_in_spo', label: 'SPO', sorttype: 'text', align: 'center', editable: true, edittype: 'checkbox', editoptions: { value: "Y:N", required: true} },
                        { name: 'include_in_bolt', label: 'BOLT', sorttype: 'text', align: 'center', editable: true, edittype: 'checkbox', editoptions: { value: "Y:N", required: true} }
                      ],
        jsonReader: {
            root: "rows",
            page: "currentPage",
            total: "totalPages",
            records: "totalRows",
            id: "seg_code", //set grid row id by column model zero based index or name
            repeatitems: false
        },
        onSelectRow: function (rowid, status) {
            if (rowid && rowid !== lastsel) {
                jQuery('#listSegcode').jqGrid('restoreRow', lastsel);
                jQuery('#listSegcode').jqGrid('editRow', rowid, true);
                lastsel = rowid;

                //Enable Row Buttons
                jQuery('#cancel_' + rowid).button("enable");
                jQuery('#save_' + rowid).button("enable");
            }
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
                postData.id = "new";
                postData.oper = "new";
            }

            //Convert JSON to JSONstring
            var postDataString = JSON.stringify(postData);
            return postDataString;
        }
    });

    //Add Navigation Bar
    jQuery("#listSegcode").jqGrid('navGrid', '#pagerSegcode',
        {/*navGrid options*/
            cloneToTop: true,
            edit: false,
            add: false,
            del: false,
            search: false,
            refresh: true
        }
        ).navButtonAdd('#listSegcode_toppager_left', {
            caption: "",
            title: "Add",
            buttonicon: "ui-icon-plus",
            onClickButton: function () {
                //Get selected row from table
                var seg_sel_id = jQuery('#listSegcode').jqGrid('getGridParam', 'selrow');

                //If currently editing a row(has a selected row), do not add new row
                if (!seg_sel_id) {
                    //set lastsel and call grid new row function
                    lastsel = "new_row";
                    jQuery("#listSegcode").jqGrid('addRow', { rowID: "new_row" });
                }
                else {
                    showErrorDialog('Please finish editing your current order.');
                }

                //no postback
                return false;
            },
            position: "last"
        }).navButtonAdd('#listSegcode_toppager_left', {
            caption: "",
            title: "Delete",
            buttonicon: "ui-icon-trash",
            onClickButton: function () {
                //Get selected row from orders table
                var seg_sel_id = $('#listSegcode').jqGrid('getGridParam', 'selrow');
                if (seg_sel_id) {
                    //Show dialog
                    $("#confirm-segcode-delete").dialog("open");
                }

                //Prevent default action of following link
                return false;
            },
            position: "last"
        });
});

/* Shared Functions */
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
        var grid0 = $("#listSegcode");
        grid0.setGridWidth($("#segcodeGridWrapper").width());
    }
};

</script>
</asp:Content>
