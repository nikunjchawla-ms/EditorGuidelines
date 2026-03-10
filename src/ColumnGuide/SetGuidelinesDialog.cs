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
            Size = new Size(500, 280);

            var guidelinesLabel = new Label
            {
                Text = "Guidelines:",
                Location = new Point(12, 15),
                AutoSize = true
            };

            _guidelinesTextBox = new TextBox
            {
                Location = new Point(12, 35),
                Size = new Size(460, 23),
                Text = currentGuidelines ?? string.Empty
            };
            _guidelinesTextBox.TextChanged += OnTextChanged;

            var defaultStyleLabel = new Label
            {
                Text = "Default style:",
                Location = new Point(12, 70),
                AutoSize = true
            };

            _defaultStyleTextBox = new TextBox
            {
                Location = new Point(12, 90),
                Size = new Size(460, 23),
                Text = currentDefaultStyle ?? string.Empty
            };
            _defaultStyleTextBox.TextChanged += OnTextChanged;

            _validationLabel = new Label
            {
                Location = new Point(12, 125),
                Size = new Size(460, 60),
                ForeColor = Color.DarkRed,
                Text = string.Empty
            };

            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(316, 200),
                Size = new Size(75, 23)
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(397, 200),
                Size = new Size(75, 23)
            };

            AcceptButton = _okButton;
            CancelButton = _cancelButton;

            Controls.AddRange(new Control[]
            {
                guidelinesLabel, _guidelinesTextBox,
                defaultStyleLabel, _defaultStyleTextBox,
                _validationLabel,
                _okButton, _cancelButton
            });

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
