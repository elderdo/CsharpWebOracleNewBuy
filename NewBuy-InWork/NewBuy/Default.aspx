<%@ Page Title="Home" Language="C#" MasterPageFile="~/NewBuy.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="NewBuy.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="mainBodyContentText">
        <% if (HttpContext.Current.Session["Connection"] == null)
           {%>
                <p>Welcome, please login below.</p>
                <asp:Label ID="LoginFeedback" runat="server" Text=""></asp:Label>
                <div class="loginDiv ui-widget">
                    <label>User: </label><br />
                    <asp:TextBox ID="Username" CssClass="loginDivInput" runat="server" Text=""></asp:TextBox>
                    <label>Password: </label><br />
                    <asp:TextBox ID="Password" CssClass="loginDivInput" TextMode="Password" runat="server"></asp:TextBox><br />
                    <label>Database: </label><br />
                    <asp:DropDownList ID="Database" CssClass="loginDivDropDown" runat="server" OnSelectedIndexChanged="Database_SelectedIndexChanged">
                        <asp:ListItem Selected="True">Prod</asp:ListItem>
                        <asp:ListItem>Test</asp:ListItem>
                        <asp:ListItem>Dev</asp:ListItem>
                    </asp:DropDownList>
                    <div class="loginDivSubmit">
                        <asp:Button ID="Submit" runat="server" Text="Login" onclick="Submit_Click" 
                            CssClass="ui-button ui-state-default ui-corner-all" />
                    </div>
                </div>
         <%}
           else { %>
               <p>Welcome, select a menu item above to continue.</p>
           <%}%>
    </div>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContentjQuery" runat="server">
    <% if (HttpContext.Current.Session["Connection"] == null)
   {%>
        <script type="text/javascript">
            $(function () {
                //hover states
                $('#<%= Submit.ClientID %>').hover(
                    function () { $(this).addClass('ui-state-hover'); },
                    function () { $(this).removeClass('ui-state-hover'); }
                );
            });
        </script>
 <%}%>
</asp:Content>
