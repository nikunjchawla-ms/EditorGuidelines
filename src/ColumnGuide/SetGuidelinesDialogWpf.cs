using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace EditorGuidelines
{
    /// <summary>
    /// WPF dialog for setting guideline positions and styles.
    /// Uses VS theming for dark/light mode support via implicit styles.
    /// </summary>
    internal sealed class SetGuidelinesDialogWpf : DialogWindow
    {
        private readonly TextBox _guidelinesTextBox;
        private readonly TextBox _defaultStyleTextBox;
        private readonly TextBlock _validationTextBlock;

        public SetGuidelinesDialogWpf(string currentGuidelines, string currentDefaultStyle)
        {
            Title = "Set Guidelines";
            HasMaximizeButton = false;
            HasMinimizeButton = false;
            HasHelpButton = false;
            HasDialogFrame = true;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.Height;
            Width = 500;
            MinWidth = 500;
            MaxWidth = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Apply VS themed implicit styles to all standard controls in this dialog
            ApplyVsThemedStyles();

            var grid = new Grid { Margin = new Thickness(16) };
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Guidelines label
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Guidelines textbox
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Guidelines hint
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) }); // Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Default style label
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Default style textbox
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Default style hint
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) }); // Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Doc link
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) }); // Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Validation
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) }); // Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Guidelines
            var guidelinesLabel = new TextBlock { Text = "Guidelines:", Margin = new Thickness(0, 0, 0, 4) };
            Grid.SetRow(guidelinesLabel, 0);
            grid.Children.Add(guidelinesLabel);

            _guidelinesTextBox = new TextBox { Text = currentGuidelines ?? string.Empty };
            _guidelinesTextBox.TextChanged += OnTextChanged;
            Grid.SetRow(_guidelinesTextBox, 1);
            grid.Children.Add(_guidelinesTextBox);

            var guidelinesHint = CreateHintLabel("e.g. 80, 120 or 80 1px solid red, 120 2px dashed blue");
            Grid.SetRow(guidelinesHint, 2);
            grid.Children.Add(guidelinesHint);

            // Default style
            var defaultStyleLabel = new TextBlock { Text = "Default style:", Margin = new Thickness(0, 0, 0, 4) };
            Grid.SetRow(defaultStyleLabel, 4);
            grid.Children.Add(defaultStyleLabel);

            _defaultStyleTextBox = new TextBox { Text = currentDefaultStyle ?? string.Empty };
            _defaultStyleTextBox.TextChanged += OnTextChanged;
            Grid.SetRow(_defaultStyleTextBox, 5);
            grid.Children.Add(_defaultStyleTextBox);

            var defaultStyleHint = CreateHintLabel("e.g., if set to 1px dotted gold, that style will apply to guidelines without an explicit style specified");
            Grid.SetRow(defaultStyleHint, 6);
            grid.Children.Add(defaultStyleHint);

            // Documentation link
            var docLink = new TextBlock { Margin = new Thickness(0, 0, 0, 0) };
            var hyperlink = new System.Windows.Documents.Hyperlink { NavigateUri = new Uri("https://github.com/pharring/EditorGuidelines#editorconfig-support-vs-2017-or-above") };
            hyperlink.Inlines.Add("Syntax documentation");
            hyperlink.RequestNavigate += (s, ev) => { System.Diagnostics.Process.Start(ev.Uri.AbsoluteUri); ev.Handled = true; };
            hyperlink.SetResourceReference(System.Windows.Documents.Hyperlink.ForegroundProperty, VsBrushes.ControlLinkTextKey);
            docLink.Inlines.Add(hyperlink);
            Grid.SetRow(docLink, 8);
            grid.Children.Add(docLink);

            // Validation
            _validationTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = Brushes.IndianRed
            };
            Grid.SetRow(_validationTextBlock, 10);
            grid.Children.Add(_validationTextBlock);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                IsDefault = true,
                MinWidth = 75,
                Padding = new Thickness(12, 4, 12, 4)
            };
            okButton.Click += (s, ev) => { DialogResult = true; };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Cancel",
                IsCancel = true,
                MinWidth = 75,
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(8, 0, 0, 0)
            };
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 12);
            grid.Children.Add(buttonPanel);

            Content = grid;
            ValidateInput();
        }

        /// <summary>
        /// The guidelines string entered by the user.
        /// </summary>
        public string GuidelinesText => _guidelinesTextBox.Text.Trim();

        /// <summary>
        /// The default style string entered by the user.
        /// </summary>
        public string DefaultStyleText => _defaultStyleTextBox.Text.Trim();

        /// <summary>
        /// Apply VS themed implicit styles for standard controls.
        /// Sets background/foreground on the window and adds implicit styles for
        /// TextBlock, TextBox, Button, and CheckBox so child controls inherit VS theme colors.
        /// </summary>
        private void ApplyVsThemedStyles()
        {
            SetResourceReference(BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
            SetResourceReference(ForegroundProperty, VsBrushes.ToolWindowTextKey);

            var textBlockStyle = new Style(typeof(TextBlock));
            textBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension(VsBrushes.ToolWindowTextKey)));
            Resources[typeof(TextBlock)] = textBlockStyle;

            var textBoxStyle = new Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, new DynamicResourceExtension(VsBrushes.WindowKey)));
            textBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, new DynamicResourceExtension(VsBrushes.WindowTextKey)));
            textBoxStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, new DynamicResourceExtension(VsBrushes.ActiveBorderKey)));
            Resources[typeof(TextBox)] = textBoxStyle;

            var buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new DynamicResourceExtension(VsBrushes.ButtonFaceKey)));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, new DynamicResourceExtension(VsBrushes.ButtonTextKey)));
            buttonStyle.Setters.Add(new Setter(Button.MinHeightProperty, 23.0));
            Resources[typeof(Button)] = buttonStyle;
        }

        private static TextBlock CreateHintLabel(string text)
        {
            var label = new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.GrayTextKey);
            return label;
        }

        /// <summary>
        /// After the window handle is created, set the title bar to dark mode if the VS theme is dark.
        /// Uses the Windows DWM API (DWMWA_USE_IMMERSIVE_DARK_MODE).
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyTitleBarTheme();
        }

        private void ApplyTitleBarTheme()
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource == null)
            {
                return;
            }

            // Detect if VS is using a dark theme by checking the background brightness
            var bgBrush = TryFindResource(VsBrushes.ToolWindowBackgroundKey) as SolidColorBrush;
            if (bgBrush == null)
            {
                return;
            }

            var color = bgBrush.Color;
            // Perceived brightness: dark themes have low values
            var brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
            var useDarkMode = brightness < 0.5;

            // DWMWA_USE_IMMERSIVE_DARK_MODE = 20
            var value = useDarkMode ? 1 : 0;
            DwmSetWindowAttribute(hwndSource.Handle, 20, ref value, sizeof(int));
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int attrSize);

        private void OnTextChanged(object sender, TextChangedEventArgs e) => ValidateInput();

        private void ValidateInput()
        {
            var errors = new List<string>();

            var guidelinesText = _guidelinesTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(guidelinesText))
            {
                var result = Parser.ParseGuidelinesFromCodingConvention(guidelinesText, null);
                if (result == null || result.Count == 0)
                {
                    errors.Add("Guidelines: Could not parse the guideline entries. Expected comma-separated column numbers with optional style parameters.");
                }
            }

            var defaultStyleText = _defaultStyleTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(defaultStyleText))
            {
                if (!Parser.TryParseStrokeParametersFromCodingConvention(defaultStyleText, out _))
                {
                    errors.Add("Default style: Could not parse the style. Expected format matching the .editorconfig guidelines_style syntax.");
                }
            }

            _validationTextBlock.Text = errors.Count > 0
                ? string.Join(Environment.NewLine, errors)
                : string.Empty;
        }
    }
}
