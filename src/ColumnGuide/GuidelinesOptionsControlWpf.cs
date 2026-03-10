using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EditorGuidelines
{
    /// <summary>
    /// WPF control that provides the UI for the Editor Guidelines options page.
    /// Uses VS theming for dark/light mode support via implicit styles.
    /// </summary>
    internal sealed class GuidelinesOptionsControlWpf : UserControl
    {
        private readonly TextBox _guidelinesTextBox;
        private readonly TextBox _defaultStyleTextBox;
        private readonly TextBlock _validationTextBlock;
        private readonly CheckBox _ignoreEditorConfigCheckBox;
        private readonly CheckBox _ignoreEditorConfigSolutionCheckBox;
        private readonly TextBlock _solutionOverrideLabel;
        private readonly StackPanel _solutionPanel;

        public GuidelinesOptionsControlWpf()
        {
            // Apply VS themed implicit styles for all standard controls
            ApplyVsThemedStyles();

            var mainPanel = new StackPanel { Margin = new Thickness(12, 8, 16, 8) };

            // Guidelines
            mainPanel.Children.Add(new TextBlock { Text = "Guidelines:", Margin = new Thickness(0, 0, 0, 4) });

            _guidelinesTextBox = new TextBox();
            _guidelinesTextBox.TextChanged += OnTextChanged;
            mainPanel.Children.Add(_guidelinesTextBox);

            mainPanel.Children.Add(CreateHintLabel("e.g. 80, 120 or 80 1px solid red, 120 2px dashed blue"));
            mainPanel.Children.Add(new Border { Height = 12 });

            // Default style
            mainPanel.Children.Add(new TextBlock { Text = "Default style:", Margin = new Thickness(0, 0, 0, 4) });

            _defaultStyleTextBox = new TextBox();
            _defaultStyleTextBox.TextChanged += OnTextChanged;
            mainPanel.Children.Add(_defaultStyleTextBox);

            mainPanel.Children.Add(CreateHintLabel("e.g., if set to 1px dotted gold, that style will apply to guidelines without an explicit style specified. When empty, Fonts & Colors is used."));
            mainPanel.Children.Add(new Border { Height = 8 });

            // Documentation link
            var docLink = new TextBlock { Margin = new Thickness(0, 0, 0, 8) };
            var hyperlink = new System.Windows.Documents.Hyperlink
            {
                NavigateUri =
                    new Uri("https://github.com/pharring/EditorGuidelines#editorconfig-support-vs-2017-or-above")
            };
            hyperlink.Inlines.Add("Syntax documentation");
            hyperlink.RequestNavigate += (s, ev) =>
            {
                System.Diagnostics.Process.Start(ev.Uri.AbsoluteUri); ev.Handled = true;
            };
            hyperlink.Foreground = SystemColors.HotTrackBrush;
            docLink.Inlines.Add(hyperlink);
            mainPanel.Children.Add(docLink);

            // Validation
            _validationTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.IndianRed,
                MinHeight = 36,
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainPanel.Children.Add(_validationTextBlock);

            mainPanel.Children.Add(new Border { Height = 4 });

            // Global ignore checkbox
            _ignoreEditorConfigCheckBox = new CheckBox
            {
                Content = "Ignore .editorconfig guideline settings (global)",
                Margin = new Thickness(0, 0, 0, 6)
            };
            _ignoreEditorConfigCheckBox.Checked += OnIgnoreGlobalChanged;
            _ignoreEditorConfigCheckBox.Unchecked += OnIgnoreGlobalChanged;
            mainPanel.Children.Add(_ignoreEditorConfigCheckBox);

            // Per-solution ignore panel
            _solutionPanel = new StackPanel { Visibility = Visibility.Collapsed };

            _ignoreEditorConfigSolutionCheckBox = new CheckBox
            {
                Content = "Ignore .editorconfig guideline settings (this solution)",
                Margin = new Thickness(0, 0, 0, 2)
            };
            _solutionPanel.Children.Add(_ignoreEditorConfigSolutionCheckBox);

            _solutionOverrideLabel = new TextBlock
            {
                Text = "(overridden by global setting)",
                Margin = new Thickness(20, 0, 0, 0),
                FontStyle = FontStyles.Italic,
                Visibility = Visibility.Collapsed
            };
            _solutionOverrideLabel.Foreground = SystemColors.GrayTextBrush;
            _solutionPanel.Children.Add(_solutionOverrideLabel);

            mainPanel.Children.Add(_solutionPanel);
            Content = mainPanel;
        }

        public string GuidelinesText
        {
            get => _guidelinesTextBox.Text;
            set => _guidelinesTextBox.Text = value ?? string.Empty;
        }

        public string DefaultStyleText
        {
            get => _defaultStyleTextBox.Text;
            set => _defaultStyleTextBox.Text = value ?? string.Empty;
        }

        public bool IgnoreEditorConfig
        {
            get => _ignoreEditorConfigCheckBox.IsChecked == true;
            set
            {
                _ignoreEditorConfigCheckBox.IsChecked = value;
                UpdateSolutionCheckBoxState();
            }
        }

        public bool IgnoreEditorConfigSolution
        {
            get => _ignoreEditorConfigSolutionCheckBox.IsChecked == true;
            set => _ignoreEditorConfigSolutionCheckBox.IsChecked = value;
        }

        public bool ShowSolutionCheckBox
        {
            set
            {
                _solutionPanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                UpdateSolutionCheckBoxState();
            }
        }

        private void OnIgnoreGlobalChanged(object sender, RoutedEventArgs e) => UpdateSolutionCheckBoxState();

        private void UpdateSolutionCheckBoxState()
        {
            var globalOn = _ignoreEditorConfigCheckBox.IsChecked == true;
            _ignoreEditorConfigSolutionCheckBox.IsEnabled = !globalOn;
            _solutionOverrideLabel.Visibility = _solutionPanel.Visibility == Visibility.Visible && globalOn
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// The legacy Options dialog in VS always uses its own light-themed background.
        /// We don't override background or text colors here; WPF system defaults
        /// provide dark-on-light rendering that matches the host dialog.
        /// </summary>
        private void ApplyVsThemedStyles()
        {
        }

        private TextBlock CreateHintLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = SystemColors.GrayTextBrush
            };
        }

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
                    errors.Add("Guidelines: Could not parse the guideline entries.");
                }
            }

            var defaultStyleText = _defaultStyleTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(defaultStyleText))
            {
                if (!Parser.TryParseStrokeParametersFromCodingConvention(defaultStyleText, out _))
                {
                    errors.Add("Default style: Could not parse the style.");
                }
            }

            _validationTextBlock.Text = errors.Count > 0
                ? string.Join(Environment.NewLine, errors)
                : string.Empty;
        }
    }
}
