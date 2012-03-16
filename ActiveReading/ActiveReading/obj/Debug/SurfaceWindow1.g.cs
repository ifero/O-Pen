﻿#pragma checksum "..\..\SurfaceWindow1.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "C2255B872A7474EEE3C0F3255515EF78"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.261
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Controls.Primitives;
using Microsoft.Surface.Presentation.Controls.TouchVisualizations;
using Microsoft.Surface.Presentation.Input;
using Microsoft.Surface.Presentation.Palettes;
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


namespace ActiveReading {
    
    
    /// <summary>
    /// SurfaceWindow1
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
    public partial class SurfaceWindow1 : Microsoft.Surface.Presentation.Controls.SurfaceWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 27 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Microsoft.Surface.Presentation.Controls.SurfaceInkCanvas annotateBoard;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Microsoft.Surface.Presentation.Controls.SurfaceInkCanvas highlightBoard;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label highlightLabel;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label annotateLabel;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label inkLable;
        
        #line default
        #line hidden
        
        
        #line 34 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Microsoft.Surface.Presentation.Controls.SurfaceButton modeButton;
        
        #line default
        #line hidden
        
        
        #line 37 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Microsoft.Surface.Presentation.Controls.SurfaceButton highlightButton;
        
        #line default
        #line hidden
        
        
        #line 40 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Microsoft.Surface.Presentation.Controls.SurfaceButton taskButton;
        
        #line default
        #line hidden
        
        
        #line 43 "..\..\SurfaceWindow1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Microsoft.Surface.Presentation.Controls.SurfaceButton annotateButton;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/ActiveReading;component/surfacewindow1.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\SurfaceWindow1.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.annotateBoard = ((Microsoft.Surface.Presentation.Controls.SurfaceInkCanvas)(target));
            
            #line 28 "..\..\SurfaceWindow1.xaml"
            this.annotateBoard.TouchDown += new System.EventHandler<System.Windows.Input.TouchEventArgs>(this.onTouchDown);
            
            #line default
            #line hidden
            
            #line 28 "..\..\SurfaceWindow1.xaml"
            this.annotateBoard.TouchMove += new System.EventHandler<System.Windows.Input.TouchEventArgs>(this.onTouchDown);
            
            #line default
            #line hidden
            
            #line 28 "..\..\SurfaceWindow1.xaml"
            this.annotateBoard.PreviewTouchDown += new System.EventHandler<System.Windows.Input.TouchEventArgs>(this.onTouchDown);
            
            #line default
            #line hidden
            return;
            case 2:
            this.highlightBoard = ((Microsoft.Surface.Presentation.Controls.SurfaceInkCanvas)(target));
            
            #line 30 "..\..\SurfaceWindow1.xaml"
            this.highlightBoard.TouchDown += new System.EventHandler<System.Windows.Input.TouchEventArgs>(this.onTouchDown);
            
            #line default
            #line hidden
            
            #line 30 "..\..\SurfaceWindow1.xaml"
            this.highlightBoard.TouchMove += new System.EventHandler<System.Windows.Input.TouchEventArgs>(this.onTouchDown);
            
            #line default
            #line hidden
            
            #line 30 "..\..\SurfaceWindow1.xaml"
            this.highlightBoard.PreviewTouchDown += new System.EventHandler<System.Windows.Input.TouchEventArgs>(this.onTouchDown);
            
            #line default
            #line hidden
            return;
            case 3:
            this.highlightLabel = ((System.Windows.Controls.Label)(target));
            return;
            case 4:
            this.annotateLabel = ((System.Windows.Controls.Label)(target));
            return;
            case 5:
            this.inkLable = ((System.Windows.Controls.Label)(target));
            return;
            case 6:
            this.modeButton = ((Microsoft.Surface.Presentation.Controls.SurfaceButton)(target));
            
            #line 36 "..\..\SurfaceWindow1.xaml"
            this.modeButton.Click += new System.Windows.RoutedEventHandler(this.onModeClick);
            
            #line default
            #line hidden
            return;
            case 7:
            this.highlightButton = ((Microsoft.Surface.Presentation.Controls.SurfaceButton)(target));
            
            #line 39 "..\..\SurfaceWindow1.xaml"
            this.highlightButton.Click += new System.Windows.RoutedEventHandler(this.onHlClick);
            
            #line default
            #line hidden
            return;
            case 8:
            this.taskButton = ((Microsoft.Surface.Presentation.Controls.SurfaceButton)(target));
            
            #line 42 "..\..\SurfaceWindow1.xaml"
            this.taskButton.Click += new System.Windows.RoutedEventHandler(this.onTaskClick);
            
            #line default
            #line hidden
            return;
            case 9:
            this.annotateButton = ((Microsoft.Surface.Presentation.Controls.SurfaceButton)(target));
            
            #line 45 "..\..\SurfaceWindow1.xaml"
            this.annotateButton.Click += new System.Windows.RoutedEventHandler(this.onAnnotateClick);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

