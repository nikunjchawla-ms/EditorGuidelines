// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.Text;

using static System.Globalization.CultureInfo;
using static EditorGuidelines.Guideline;

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread. SettingsStore is thread safe.
namespace EditorGuidelines
{
    [Export(typeof(ITextEditorGuidesSettings))]
    [Export(typeof(ITextEditorGuidesSettingsChanger))]
    internal sealed class TextEditorGuidesSettings : ITextEditorGuidesSettings, INotifyPropertyChanged, ITextEditorGuidesSettingsChanger
    {
        private const int c_maxGuides = 12;

        [Import]
        private Lazy<HostServices> HostServices { get; set; }

        private IVsSettingsStore ReadOnlyUserSettings
        {
            get
            {
                var manager = HostServices.Value.SettingsManagerService;
                Marshal.ThrowExceptionForHR(manager.GetReadOnlySettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out var store));
                return store;
            }
        }

        private IVsWritableSettingsStore ReadWriteUserSettings
        {
            get
            {
                var manager = HostServices.Value.SettingsManagerService;
                Marshal.ThrowExceptionForHR(manager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out var store));
                return store;
            }
        }

        private string GetUserSettingsString(string key, string value)
        {
            var store = ReadOnlyUserSettings;
            Marshal.ThrowExceptionForHR(store.GetStringOrDefault(key, value, string.Empty, out var result));
            return result;
        }

        private void WriteUserSettingsString(string key, string propertyName, string value)
        {
            var store = ReadWriteUserSettings;

            Marshal.ThrowExceptionForHR(store.CollectionExists(key, out int exists));
            if (exists == 0)
            {
                Marshal.ThrowExceptionForHR(store.CreateCollection(key));
            }

            Marshal.ThrowExceptionForHR(store.SetString(key, propertyName, value));
        }

        #region Legacy settings format (RGB(r,g,b) col1, col2, col3)

        private void WriteSettings(Color color, IEnumerable<int> columns)
        {
            var value = ComposeSettingsString(color, columns);
            GuidelinesConfiguration = value;
        }

        private static string ComposeSettingsString(Color color, IEnumerable<int> columns)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(InvariantCulture, "RGB({0},{1},{2})", color.R, color.G, color.B);
            var columnsEnumerator = columns.GetEnumerator();
            if (columnsEnumerator.MoveNext())
            {
                sb.AppendFormat(InvariantCulture, " {0}", columnsEnumerator.Current);
                while (columnsEnumerator.MoveNext())
                {
                    sb.AppendFormat(InvariantCulture, ", {0}", columnsEnumerator.Current);
                }
            }

            return sb.ToString();
        }

        private string _guidelinesConfiguration;
        private string GuidelinesConfiguration
        {
            get
            {
                if (_guidelinesConfiguration == null)
                {
                    _guidelinesConfiguration = GetUserSettingsString(c_textEditor, c_guidesPropertyName).Trim();
                }

                return _guidelinesConfiguration;
            }

            set
            {
                if (value != _guidelinesConfiguration)
                {
                    _guidelinesConfiguration = value;
                    WriteUserSettingsString(c_textEditor, c_guidesPropertyName, value);
                }
            }
        }

        // Parse a color out of a string that begins like "RGB(255,0,0)"
        public Color GuidelinesColor
        {
            get
            {
                var config = GuidelinesConfiguration;
                if (!string.IsNullOrEmpty(config) && config.StartsWith("RGB(", StringComparison.Ordinal))
                {
                    var lastParen = config.IndexOf(')');
                    if (lastParen > 4)
                    {
                        var rgbs = config.Substring(4, lastParen - 4).Split(',');

                        if (rgbs.Length >= 3)
                        {
                            if (byte.TryParse(rgbs[0], out var r) &&
                                byte.TryParse(rgbs[1], out var g) &&
                                byte.TryParse(rgbs[2], out var b))
                            {
                                return Color.FromRgb(r, g, b);
                            }
                        }
                    }
                }

                return Colors.DarkRed;
            }

            set => WriteSettings(value, GuideLinePositionsInChars);
        }

        // Parse a list of integer values out of a string that looks like "RGB(255,0,0) 1,5,10,80"
        public IEnumerable<int> GuideLinePositionsInChars
        {
            get
            {
                var config = GuidelinesConfiguration;
                if (string.IsNullOrEmpty(config))
                {
                    yield break;
                }

                if (!config.StartsWith("RGB(", StringComparison.Ordinal))
                {
                    yield break;
                }

                var lastParen = config.IndexOf(')');
                if (lastParen <= 4)
                {
                    yield break;
                }

                var columns = config.Substring(lastParen + 1).Split(',');

                var columnCount = 0;
                foreach (var columnText in columns)
                {
                    var column = -1;
                    if (int.TryParse(columnText, out column) && column >= 0 /*Note: VS 2008 didn't allow zero, but we do, per user request*/)
                    {
                        columnCount++;
                        yield return column;
                        if (columnCount >= c_maxGuides)
                        {
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sync the legacy Guides key with column positions extracted from StyledGuidelines.
        /// </summary>
        private void SyncLegacyFromStyled(string styledValue)
        {
            var guidelines = Parser.ParseGuidelinesFromCodingConvention(styledValue, null);
            var columns = guidelines?.Select(g => g.Column) ?? Enumerable.Empty<int>();
            GuidelinesConfiguration = ComposeSettingsString(GuidelinesColor, columns);
        }

        #endregion

        #region Styled guidelines settings

        private string _styledGuidelines;

        /// <summary>
        /// The styled guidelines configuration string.
        /// Uses the same comma-separated syntax as the .editorconfig guidelines property,
        /// in which each entry is a column number optionally followed by style parameters.
        /// </summary>
        public string StyledGuidelines
        {
            get
            {
                if (_styledGuidelines != null)
                {
                    return _styledGuidelines;
                }

                _styledGuidelines = GetUserSettingsString(c_textEditor, c_styledGuidelinesPropertyName).Trim();

                // Migration: if StyledGuidelines is empty but legacy Guides has data, migrate.
                if (string.IsNullOrEmpty(_styledGuidelines))
                {
                    var legacyColumns = new List<int>(GuideLinePositionsInChars);
                    if (legacyColumns.Count > 0)
                    {
                        _styledGuidelines = string.Join(", ", legacyColumns.Select(c => c.ToString(InvariantCulture)));
                        WriteUserSettingsString(c_textEditor, c_styledGuidelinesPropertyName, _styledGuidelines);
                    }
                }

                return _styledGuidelines;
            }

            set
            {
                if (value == _styledGuidelines)
                {
                    return;
                }

                _styledGuidelines = value;
                WriteUserSettingsString(c_textEditor, c_styledGuidelinesPropertyName, value);

                // Keep legacy key in sync with column positions
                SyncLegacyFromStyled(value);

                FirePropertyChanged(nameof(StyledGuidelines));
                FirePropertyChanged(nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars));
            }
        }

        private string _defaultGuidelineStyle;

        /// <summary>
        /// The default guideline style string applied to guidelines without an explicit style.
        /// Uses the same syntax as the .editorconfig guidelines_style property.
        /// When empty, the Fonts &amp; Colors brush is used.
        /// </summary>
        public string DefaultGuidelineStyle
        {
            get
            {
                if (_defaultGuidelineStyle == null)
                {
                    _defaultGuidelineStyle = GetUserSettingsString(c_textEditor, c_defaultGuidelineStylePropertyName).Trim();
                }

                return _defaultGuidelineStyle;
            }

            set
            {
                if (value == _defaultGuidelineStyle)
                {
                    return;
                }

                _defaultGuidelineStyle = value;
                WriteUserSettingsString(c_textEditor, c_defaultGuidelineStylePropertyName, value);
                FirePropertyChanged(nameof(DefaultGuidelineStyle));
                // Also signal that guidelines rendering may need to change
                FirePropertyChanged(nameof(StyledGuidelines));
            }
        }

        /// <summary>
        /// Get the styled guidelines as parsed <see cref="Guideline"/> objects.
        /// Falls back to legacy column-only guidelines if StyledGuidelines is empty.
        /// </summary>
        public IEnumerable<Guideline> StyledGuidelineObjects
        {
            get
            {
                var styled = StyledGuidelines;
                if (!string.IsNullOrEmpty(styled))
                {
                    // Parse the default style
                    StrokeParameters fallbackStyle = null;
                    var defaultStyle = DefaultGuidelineStyle;
                    if (!string.IsNullOrEmpty(defaultStyle))
                    {
                        Parser.TryParseStrokeParametersFromCodingConvention(defaultStyle, out fallbackStyle);
                        fallbackStyle?.Freeze();
                    }

                    var guidelines = Parser.ParseGuidelinesFromCodingConvention(styled, fallbackStyle);
                    if (guidelines != null)
                    {
                        return guidelines;
                    }
                }

                // Fall back to legacy: column numbers with null stroke parameters (uses Fonts & Colors brush)
                return GuideLinePositionsInChars.Select(c => new Guideline(c, null));
            }
        }

        #endregion

        #region ITextEditorGuidesSettingsChanger Members

        public bool AddGuideline(int column)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException(nameof(column), Resources.AddGuidelineParameterOutOfRange);
            }

            if (GetCountOfGuidelines() >= c_maxGuides)
            {
                return false; // Cannot add more than _maxGuides guidelines
            }

            // Check for duplicates
            if (IsGuidelinePresent(column))
            {
                return false;
            }

            // Add to styled guidelines, preserving existing styles
            var styled = StyledGuidelines;
            if (string.IsNullOrEmpty(styled))
            {
                StyledGuidelines = column.ToString(InvariantCulture);
            }
            else
            {
                StyledGuidelines = styled + ", " + column.ToString(InvariantCulture);
            }

            return true;
        }

        public bool RemoveGuideline(int column)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException(nameof(column), Resources.RemoveGuidelineParameterOutOfRange);
            }

            var styled = StyledGuidelines;
            if (string.IsNullOrEmpty(styled))
            {
                return false;
            }

            // Parse current guidelines, remove the one at the given column
            StrokeParameters fallbackStyle = null;
            var defaultStyle = DefaultGuidelineStyle;
            if (!string.IsNullOrEmpty(defaultStyle))
            {
                Parser.TryParseStrokeParametersFromCodingConvention(defaultStyle, out fallbackStyle);
            }

            var guidelines = Parser.ParseGuidelinesFromCodingConvention(styled, fallbackStyle);
            if (guidelines == null)
            {
                return false;
            }

            var toRemove = guidelines.FirstOrDefault(g => g.Column == column);
            if (toRemove == null)
            {
                // Not present. Allow user to remove the last column even if they're not on the right column.
                if (guidelines.Count != 1)
                {
                    return false;
                }

                guidelines.Clear();
            }
            else
            {
                guidelines.Remove(toRemove);
            }

            StyledGuidelines = ComposeStyledString(guidelines);
            return true;
        }

        public bool CanAddGuideline(int column)
            => IsValidColumn(column)
            && GetCountOfGuidelines() < c_maxGuides
            && !IsGuidelinePresent(column);

        public bool CanRemoveGuideline(int column)
            => IsValidColumn(column)
            && (IsGuidelinePresent(column) || HasExactlyOneGuideline()); // Allow user to remove the last guideline regardless of the column

        public void RemoveAllGuidelines()
            => StyledGuidelines = string.Empty;

        /// <summary>
        /// Compose a styled guidelines string from a set of <see cref="Guideline"/> objects.
        /// </summary>
        internal static string ComposeStyledString(IEnumerable<Guideline> guidelines)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var guideline in guidelines)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(guideline.Column.ToString(InvariantCulture));

                if (guideline.StrokeParameters != null)
                {
                    sb.AppendFormat(InvariantCulture, " {0}", guideline.StrokeParameters);
                }

                first = false;
            }

            return sb.ToString();
        }

        #endregion

        private bool HasExactlyOneGuideline()
        {
            var guidelines = StyledGuidelineObjects;
            using (var enumerator = guidelines.GetEnumerator())
            {
                return enumerator.MoveNext() && !enumerator.MoveNext();
            }
        }

        private int GetCountOfGuidelines()
        {
            var i = 0;
            foreach (var _ in StyledGuidelineObjects)
            {
                i++;
            }

            return i;
        }

        private bool IsGuidelinePresent(int column)
        {
            foreach (var guideline in StyledGuidelineObjects)
            {
                if (guideline.Column == column)
                {
                    return true;
                }
            }

            return false;
        }

        private void FirePropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Whether to globally ignore .editorconfig guideline settings.
        /// </summary>
        public bool IgnoreEditorConfigGuidelines
        {
            get
            {
                var store = ReadOnlyUserSettings;
                Marshal.ThrowExceptionForHR(store.GetBoolOrDefault(c_textEditor, c_ignoreEditorConfigPropertyName, 0, out int value));
                return value != 0;
            }

            set
            {
                var store = ReadWriteUserSettings;
                Marshal.ThrowExceptionForHR(store.SetBool(c_textEditor, c_ignoreEditorConfigPropertyName, value ? 1 : 0));
                FirePropertyChanged(nameof(IgnoreEditorConfigGuidelines));
            }
        }

        public bool DontShowVsVersionWarning
        {
            get
            {
                var store = ReadOnlyUserSettings;
                Marshal.ThrowExceptionForHR(store.GetBoolOrDefault(c_textEditor, c_dontShowVsVersionWarningPropertyName, 0, out int value));
                return value != 0;
            }

            set
            {
                var store = ReadWriteUserSettings;
                Marshal.ThrowExceptionForHR(store.SetBool(c_textEditor, c_dontShowVsVersionWarningPropertyName, value ? 1 : 0));
            }
        }

        private const string c_textEditor = "Text Editor";
        private const string c_guidesPropertyName = "Guides";
        private const string c_styledGuidelinesPropertyName = "StyledGuidelines";
        private const string c_defaultGuidelineStylePropertyName = "DefaultGuidelineStyle";
        private const string c_ignoreEditorConfigPropertyName = "IgnoreEditorConfigGuidelines";
        private const string c_dontShowVsVersionWarningPropertyName = "DontShowEditorGuidelinesVsVersionWarning";

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
