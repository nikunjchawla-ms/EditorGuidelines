using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace EditorGuidelines
{
    /// <summary>
    /// Options page for Editor Guidelines, accessible via Tools > Options > Text Editor > Editor Guidelines.
    /// </summary>
    [Guid("5aa4cf31-6030-4655-99e7-239b331103f4")]
    internal sealed class GuidelinesOptionsPage : DialogPage
    {
        private GuidelinesOptionsControl _control;

        protected override IWin32Window Window
        {
            get
            {
                if (_control == null)
                {
                    _control = new GuidelinesOptionsControl();
                }

                return _control;
            }
        }

        private ITextEditorGuidesSettings GetSettings()
        {
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            return componentModel?.GetService<ITextEditorGuidesSettings>();
        }

        /// <summary>
        /// Called when the options page is activated (shown to the user).
        /// Load settings into the control.
        /// </summary>
        protected override void OnActivate(System.ComponentModel.CancelEventArgs e)
        {
            base.OnActivate(e);

            var settings = GetSettings();
            if (settings != null && _control != null)
            {
                _control.GuidelinesText = settings.StyledGuidelines ?? string.Empty;
                _control.DefaultStyleText = settings.DefaultGuidelineStyle ?? string.Empty;
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
            }

            base.OnApply(e);
        }
    }
}
