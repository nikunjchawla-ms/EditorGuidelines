using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace EditorGuidelines
{
    /// <summary>
    /// Options page for Editor Guidelines, accessible via Tools > Options > Text Editor > Editor Guidelines.
    /// Uses WPF for VS theme (dark/light mode) support.
    /// </summary>
    [Guid("5aa4cf31-6030-4655-99e7-239b331103f4")]
    internal sealed class GuidelinesOptionsPage : UIElementDialogPage
    {
        private GuidelinesOptionsControlWpf _control;

        protected override UIElement Child
        {
            get
            {
                if (_control == null)
                {
                    _control = new GuidelinesOptionsControlWpf();
                }

                return _control;
            }
        }

        private ITextEditorGuidesSettings GetSettings()
        {
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            return componentModel?.GetService<ITextEditorGuidesSettings>();
        }

        private SolutionSettings GetSolutionSettings()
        {
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            return componentModel?.GetService<SolutionSettings>();
        }

        /// <summary>
        /// Called when the options page is activated (shown to the user).
        /// Load settings into the control.
        /// </summary>
        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            var settings = GetSettings();
            if (settings != null && _control != null)
            {
                _control.GuidelinesText = settings.StyledGuidelines ?? string.Empty;
                _control.DefaultStyleText = settings.DefaultGuidelineStyle ?? string.Empty;
                _control.IgnoreEditorConfig = settings.IgnoreEditorConfigGuidelines;
            }

            var solutionSettings = GetSolutionSettings();
            if (solutionSettings != null && _control != null)
            {
                _control.ShowSolutionCheckBox = solutionSettings.HasSolution;
                _control.IgnoreEditorConfigSolution = solutionSettings.IgnoreEditorConfigGuidelines;
            }
            else if (_control != null)
            {
                _control.ShowSolutionCheckBox = false;
            }
        }

        /// <summary>
        /// Called when the user clicks OK or Apply.
        /// Save settings from the control.
        /// </summary>
        protected override void OnApply(PageApplyEventArgs e)
        {
            var settings = GetSettings();
            if (settings != null && _control != null)
            {
                settings.StyledGuidelines = _control.GuidelinesText.Trim();
                settings.DefaultGuidelineStyle = _control.DefaultStyleText.Trim();
                settings.IgnoreEditorConfigGuidelines = _control.IgnoreEditorConfig;
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            var solutionSettings = GetSolutionSettings();
            if (solutionSettings != null && _control != null && solutionSettings.HasSolution)
            {
                solutionSettings.IgnoreEditorConfigGuidelines = _control.IgnoreEditorConfigSolution;
            }

            base.OnApply(e);
        }
    }
}
