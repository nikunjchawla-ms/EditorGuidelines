// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EditorGuidelines
{
    /// <summary>
    /// WinForms control that provides the UI for the Editor Guidelines options page.
    /// </summary>
    internal sealed class GuidelinesOptionsControl : UserControl
    {
        private readonly TextBox _guidelinesTextBox;
        private readonly TextBox _defaultStyleTextBox;
        private readonly Label _validationLabel;
        private readonly CheckBox _ignoreEditorConfigCheckBox;
        private readonly CheckBox _ignoreEditorConfigSolutionCheckBox;
        private readonly Label _solutionOverrideLabel;

        public GuidelinesOptionsControl()
        {
            AutoScaleMode = AutoScaleMode.Font;
            Size = new Size(500, 300);

            var guidelinesLabel = new Label
            {
                Text = "Guidelines:",
                Location = new Point(0, 5),
                AutoSize = true
            };

            _guidelinesTextBox = new TextBox
            {
                Location = new Point(0, 25),
                Size = new Size(480, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _guidelinesTextBox.TextChanged += OnTextChanged;

            var guidelinesHint = new Label
            {
                Text = "Comma-separated column positions, optionally with style parameters.",
                Location = new Point(0, 52),
                AutoSize = true,
                ForeColor = SystemColors.GrayText
            };

            var defaultStyleLabel = new Label
            {
                Text = "Default style:",
                Location = new Point(0, 80),
                AutoSize = true
            };

            _defaultStyleTextBox = new TextBox
            {
                Location = new Point(0, 100),
                Size = new Size(480, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _defaultStyleTextBox.TextChanged += OnTextChanged;

            var defaultStyleHint = new Label
            {
                Text = "Fallback style for guidelines without an explicit style. When empty, Fonts && Colors is used.",
                Location = new Point(0, 127),
                AutoSize = true,
                ForeColor = SystemColors.GrayText
            };

            _validationLabel = new Label
            {
                Location = new Point(0, 155),
                Size = new Size(480, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.DarkRed,
                Text = string.Empty
            };

            _ignoreEditorConfigCheckBox = new CheckBox
            {
                Text = "Ignore .editorconfig guideline settings (global)",
                Location = new Point(0, 205),
                AutoSize = true
            };
            _ignoreEditorConfigCheckBox.CheckedChanged += OnIgnoreGlobalChanged;

            _ignoreEditorConfigSolutionCheckBox = new CheckBox
            {
                Text = "Ignore .editorconfig guideline settings (this solution)",
                Location = new Point(0, 230),
                AutoSize = true,
                Visible = false
            };

            _solutionOverrideLabel = new Label
            {
                Text = "(overridden by global setting)",
                Location = new Point(20, 250),
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Visible = false
            };

            Controls.AddRange(new Control[]
            {
                guidelinesLabel, _guidelinesTextBox, guidelinesHint,
                defaultStyleLabel, _defaultStyleTextBox, defaultStyleHint,
                _validationLabel,
                _ignoreEditorConfigCheckBox,
                _ignoreEditorConfigSolutionCheckBox,
                _solutionOverrideLabel
            });
        }

        /// <summary>
        /// Gets or sets the guidelines text.
        /// </summary>
        public string GuidelinesText
        {
            get => _guidelinesTextBox.Text;
            set => _guidelinesTextBox.Text = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the default style text.
        /// </summary>
        public string DefaultStyleText
        {
            get => _defaultStyleTextBox.Text;
            set => _defaultStyleTextBox.Text = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the global ignore .editorconfig setting.
        /// </summary>
        public bool IgnoreEditorConfig
        {
            get => _ignoreEditorConfigCheckBox.Checked;
            set
            {
                _ignoreEditorConfigCheckBox.Checked = value;
                UpdateSolutionCheckBoxState();
            }
        }

        /// <summary>
        /// Gets or sets the per-solution ignore .editorconfig setting.
        /// </summary>
        public bool IgnoreEditorConfigSolution
        {
            get => _ignoreEditorConfigSolutionCheckBox.Checked;
            set => _ignoreEditorConfigSolutionCheckBox.Checked = value;
        }

        /// <summary>
        /// Whether to show the per-solution checkbox (hidden when no solution is open).
        /// </summary>
        public bool ShowSolutionCheckBox
        {
            set
            {
                _ignoreEditorConfigSolutionCheckBox.Visible = value;
                UpdateSolutionCheckBoxState();
            }
        }

        private void OnIgnoreGlobalChanged(object sender, EventArgs e)
        {
            UpdateSolutionCheckBoxState();
        }

        private void UpdateSolutionCheckBoxState()
        {
            var globalOn = _ignoreEditorConfigCheckBox.Checked;
            _ignoreEditorConfigSolutionCheckBox.Enabled = !globalOn;
            _solutionOverrideLabel.Visible = _ignoreEditorConfigSolutionCheckBox.Visible && globalOn;
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            ValidateInput();
        }

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

            _validationLabel.Text = errors.Count > 0
                ? string.Join(Environment.NewLine, errors)
                : string.Empty;
        }
    }
}
