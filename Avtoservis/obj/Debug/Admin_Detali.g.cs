﻿#pragma checksum "..\..\Admin_Detali.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "1DBE18A91974D5275B9CED939F1763B60A25377C8918668F47098EA8748E7B2C"
//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

using Avtoservis;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Avtoservis {
    
    
    /// <summary>
    /// Admin_Detali
    /// </summary>
    public partial class Admin_Detali : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IStyleConnector {
        
        
        #line 37 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnAdd;
        
        #line default
        #line hidden
        
        
        #line 38 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnDelete;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnRefresh;
        
        #line default
        #line hidden
        
        
        #line 42 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnFilter;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SearchTextBox;
        
        #line default
        #line hidden
        
        
        #line 51 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border FilterPanel;
        
        #line default
        #line hidden
        
        
        #line 70 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnCloseFilter;
        
        #line default
        #line hidden
        
        
        #line 100 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel TypeFilterPanel;
        
        #line default
        #line hidden
        
        
        #line 117 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel ManufacturerFilterPanel;
        
        #line default
        #line hidden
        
        
        #line 125 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnResetFilters;
        
        #line default
        #line hidden
        
        
        #line 139 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListView DetailsList;
        
        #line default
        #line hidden
        
        
        #line 214 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.GridViewColumn EditColumn;
        
        #line default
        #line hidden
        
        
        #line 231 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ItemsPerPageButton;
        
        #line default
        #line hidden
        
        
        #line 232 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock PaginationInfo;
        
        #line default
        #line hidden
        
        
        #line 234 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnPrevPage;
        
        #line default
        #line hidden
        
        
        #line 235 "..\..\Admin_Detali.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnNextPage;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Avtoservis;component/admin_detali.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\Admin_Detali.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 9 "..\..\Admin_Detali.xaml"
            ((Avtoservis.Admin_Detali)(target)).PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(this.Page_PreviewMouseDown);
            
            #line default
            #line hidden
            return;
            case 2:
            this.BtnAdd = ((System.Windows.Controls.Button)(target));
            
            #line 37 "..\..\Admin_Detali.xaml"
            this.BtnAdd.Click += new System.Windows.RoutedEventHandler(this.BtnAdd_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.BtnDelete = ((System.Windows.Controls.Button)(target));
            
            #line 38 "..\..\Admin_Detali.xaml"
            this.BtnDelete.Click += new System.Windows.RoutedEventHandler(this.BtnDelete_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.BtnRefresh = ((System.Windows.Controls.Button)(target));
            
            #line 39 "..\..\Admin_Detali.xaml"
            this.BtnRefresh.Click += new System.Windows.RoutedEventHandler(this.BtnRefresh_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.BtnFilter = ((System.Windows.Controls.Button)(target));
            
            #line 42 "..\..\Admin_Detali.xaml"
            this.BtnFilter.Click += new System.Windows.RoutedEventHandler(this.BtnFilter_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.SearchTextBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 48 "..\..\Admin_Detali.xaml"
            this.SearchTextBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.SearchTextBox_TextChanged);
            
            #line default
            #line hidden
            
            #line 48 "..\..\Admin_Detali.xaml"
            this.SearchTextBox.GotFocus += new System.Windows.RoutedEventHandler(this.TextBox_GotFocus);
            
            #line default
            #line hidden
            
            #line 48 "..\..\Admin_Detali.xaml"
            this.SearchTextBox.LostFocus += new System.Windows.RoutedEventHandler(this.TextBox_LostFocus);
            
            #line default
            #line hidden
            return;
            case 7:
            this.FilterPanel = ((System.Windows.Controls.Border)(target));
            return;
            case 8:
            this.BtnCloseFilter = ((System.Windows.Controls.Button)(target));
            
            #line 77 "..\..\Admin_Detali.xaml"
            this.BtnCloseFilter.Click += new System.Windows.RoutedEventHandler(this.BtnCloseFilter_Click);
            
            #line default
            #line hidden
            return;
            case 9:
            this.TypeFilterPanel = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 10:
            this.ManufacturerFilterPanel = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 11:
            this.BtnResetFilters = ((System.Windows.Controls.Button)(target));
            
            #line 133 "..\..\Admin_Detali.xaml"
            this.BtnResetFilters.Click += new System.Windows.RoutedEventHandler(this.BtnResetFilters_Click);
            
            #line default
            #line hidden
            return;
            case 12:
            this.DetailsList = ((System.Windows.Controls.ListView)(target));
            
            #line 141 "..\..\Admin_Detali.xaml"
            this.DetailsList.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(this.DetailsList_MouseDoubleClick);
            
            #line default
            #line hidden
            return;
            case 15:
            this.EditColumn = ((System.Windows.Controls.GridViewColumn)(target));
            return;
            case 17:
            this.ItemsPerPageButton = ((System.Windows.Controls.Button)(target));
            
            #line 231 "..\..\Admin_Detali.xaml"
            this.ItemsPerPageButton.Click += new System.Windows.RoutedEventHandler(this.ItemsPerPageButton_Click);
            
            #line default
            #line hidden
            return;
            case 18:
            this.PaginationInfo = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 19:
            this.BtnPrevPage = ((System.Windows.Controls.Button)(target));
            
            #line 234 "..\..\Admin_Detali.xaml"
            this.BtnPrevPage.Click += new System.Windows.RoutedEventHandler(this.BtnPrevPage_Click);
            
            #line default
            #line hidden
            return;
            case 20:
            this.BtnNextPage = ((System.Windows.Controls.Button)(target));
            
            #line 235 "..\..\Admin_Detali.xaml"
            this.BtnNextPage.Click += new System.Windows.RoutedEventHandler(this.BtnNextPage_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        void System.Windows.Markup.IStyleConnector.Connect(int connectionId, object target) {
            System.Windows.EventSetter eventSetter;
            switch (connectionId)
            {
            case 13:
            eventSetter = new System.Windows.EventSetter();
            eventSetter.Event = System.Windows.Controls.Primitives.ButtonBase.ClickEvent;
            
            #line 151 "..\..\Admin_Detali.xaml"
            eventSetter.Handler = new System.Windows.RoutedEventHandler(this.ColumnHeader_Click);
            
            #line default
            #line hidden
            ((System.Windows.Style)(target)).Setters.Add(eventSetter);
            break;
            case 14:
            eventSetter = new System.Windows.EventSetter();
            eventSetter.Event = System.Windows.Controls.Primitives.ButtonBase.ClickEvent;
            
            #line 200 "..\..\Admin_Detali.xaml"
            eventSetter.Handler = new System.Windows.RoutedEventHandler(this.ColumnHeader_Click);
            
            #line default
            #line hidden
            ((System.Windows.Style)(target)).Setters.Add(eventSetter);
            break;
            case 16:
            
            #line 217 "..\..\Admin_Detali.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.BtnEdit_Click);
            
            #line default
            #line hidden
            break;
            }
        }
    }
}

