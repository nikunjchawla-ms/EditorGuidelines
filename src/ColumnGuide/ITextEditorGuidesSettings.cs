// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace EditorGuidelines
{
    internal interface ITextEditorGuidesSettings
    {
        IEnumerable<int> GuideLinePositionsInChars { get; }

        /// <summary>
        /// The styled guidelines configuration string.
        /// Uses the same comma-separated syntax as the .editorconfig guidelines property,
        /// in which each entry is a column number optionally followed by style parameters.
        /// </summary>
        string StyledGuidelines { get; set; }

        /// <summary>
        /// The default guideline style string applied to guidelines without an explicit style.
        /// Uses the same syntax as the .editorconfig guidelines_style property.
        /// When empty, the Fonts &amp; Colors brush is used.
        /// </summary>
        string DefaultGuidelineStyle { get; set; }

        /// <summary>
        /// Get the styled guidelines as parsed <see cref="Guideline"/> objects.
        /// </summary>
        IEnumerable<Guideline> StyledGuidelineObjects { get; }

        /// <summary>
        /// Whether to globally ignore .editorconfig guideline settings.
        /// </summary>
        bool IgnoreEditorConfigGuidelines { get; set; }

        bool DontShowVsVersionWarning { get; set; }
    }
}
