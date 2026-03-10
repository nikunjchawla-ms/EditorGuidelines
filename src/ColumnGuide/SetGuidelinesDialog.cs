using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EditorGuidelines
{
    /// <summary>
    /// Dialog for setting guideline positions and styles.
    /// </summary>
    internal sealed class SetGuidelinesDialog : Form
    {
        private readonly TextBox _guidelinesTextBox;
        private readonly TextBox _defaultStyleTextBox;
        private readonly Label _validationLabel;
        private readonly Button _okButton;
        private readonly Button _cancelButton;

        public SetGuidelinesDialog(string currentGuidelines, string currentDefaultStyle)
        {
            Text = "Set Guidelines";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(10),
                AutoSize = true
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Guidelines label
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Guidelines textbox
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Default style label
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Default style textbox
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Validation label
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            var guidelinesLabel = new Label { Text = "Guidelines:", AutoSize = true };
            mainPanel.Controls.Add(guidelinesLabel, 0, 0);
            mainPanel.SetColumnSpan(guidelinesLabel, 2);

            _guidelinesTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = currentGuidelines ?? string.Empty
            };
            _guidelinesTextBox.TextChanged += OnTextChanged;
            mainPanel.Controls.Add(_guidelinesTextBox, 0, 1);
            mainPanel.SetColumnSpan(_guidelinesTextBox, 2);

            var defaultStyleLabel = new Label { Text = "Default style:", AutoSize = true };
            mainPanel.Controls.Add(defaultStyleLabel, 0, 2);
            mainPanel.SetColumnSpan(defaultStyleLabel, 2);

            _defaultStyleTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = currentDefaultStyle ?? string.Empty
            };
            _defaultStyleTextBox.TextChanged += OnTextChanged;
            mainPanel.Controls.Add(_defaultStyleTextBox, 0, 3);
            mainPanel.SetColumnSpan(_defaultStyleTextBox, 2);

            _validationLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DarkRed,
                Text = string.Empty
            };
            mainPanel.Controls.Add(_validationLabel, 0, 4);
            mainPanel.SetColumnSpan(_validationLabel, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0, 0, 5, 0)
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(75, 28)
            };

            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(75, 28)
            };

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_okButton);
            mainPanel.Controls.Add(buttonPanel, 0, 5);
            mainPanel.SetColumnSpan(buttonPanel, 2);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;

            Controls.Add(mainPanel);

            // Size to fit content, with enough width for buttons and DPI scaling
            var preferredHeight = mainPanel.PreferredSize.Height;
            ClientSize = new Size(520, preferredHeight > 0 ? preferredHeight : 260);

            // Perform initial validation
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

        private void OnTextChanged(object sender, EventArgs e)
        {
            Validate();
        }

        private void ValidateInput()
        {
            var errors = new List<string>();

            // Validate guidelines
            var guidelinesText = _guidelinesTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(guidelinesText))
            {
                var result = Parser.ParseGuidelinesFromCodingConvention(guidelinesText, null);
                if (result == null || result.Count == 0)
                {
                    errors.Add("Guidelines: Could not parse the guideline entries. Expected comma-separated column numbers with optional style parameters.");
                }
            }

            // Validate default style
            var defaultStyleText = _defaultStyleTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(defaultStyleText))
            {
                if (!Parser.TryParseStrokeParametersFromCodingConvention(defaultStyleText, out _))
                {
                    errors.Add("Default style: Could not parse the style. Expected format matching the .editorconfig guidelines_style syntax.");
                }
            }

            _validationLabel.Text = errors.Count > 0
                ? string.Join(Environment.NewLine, errors)
                : string.Empty;
        }
    }
}
