using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace EditorGuidelines
{
    /// <summary>
    /// Manages per-solution settings for Editor Guidelines, stored in the .vs folder.
    /// </summary>
    [Export]
    internal sealed class SolutionSettings : INotifyPropertyChanged, IVsPersistSolutionOpts
    {
        private const string c_settingsKey = "EditorGuidelinesSettings";
        private const string c_ignoreEditorConfigKey = "IgnoreEditorConfigGuidelines";

        private bool _ignoreEditorConfigGuidelines;
        private bool _hasSolution;

        /// <summary>
        /// The VS service provider. Fully qualified to distinguish from
        /// <see cref="Microsoft.VisualStudio.OLE.Interop.IServiceProvider"/>.
        /// </summary>
        [Import(typeof(SVsServiceProvider))]
        private System.IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Whether to ignore .editorconfig guideline settings for the current solution.
        /// </summary>
        public bool IgnoreEditorConfigGuidelines
        {
            get => _ignoreEditorConfigGuidelines;
            set
            {
                if (value == _ignoreEditorConfigGuidelines)
                {
                    return;
                }

                _ignoreEditorConfigGuidelines = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IgnoreEditorConfigGuidelines)));
                ThreadHelper.ThrowIfNotOnUIThread();
                SaveSettings();
            }
        }

        /// <summary>
        /// Whether a solution or project is currently open.
        /// </summary>
        public bool HasSolution
        {
            get => _hasSolution;
            private set
            {
                if (value == _hasSolution)
                {
                    return;
                }

                _hasSolution = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSolution)));
            }
        }

        /// <summary>
        /// Initialize the solution settings by subscribing to solution events.
        /// Must be called from the main thread after the package is sited.
        /// </summary>
        public void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ServiceProvider.GetService(typeof(SVsSolution)) is IVsSolution solution)
            {
                solution.AdviseSolutionEvents(new SolutionEventsListener(this), out _);

                // Check if a solution is already open
                ErrorHandler.Succeeded(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
                HasSolution = isOpen is bool b && b;
            }

            // Register for solution persistence
            if (ServiceProvider.GetService(typeof(SVsSolutionPersistence)) is IVsSolutionPersistence persistence)
            {
                persistence.LoadPackageUserOpts(this, c_settingsKey);
            }
        }

        private void SaveSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ServiceProvider?.GetService(typeof(SVsSolutionPersistence)) is IVsSolutionPersistence persistence)
            {
                persistence.SavePackageUserOpts(this, c_settingsKey);
            }
        }

        internal void OnSolutionOpened()
        {
            HasSolution = true;

            ThreadHelper.ThrowIfNotOnUIThread();
            if (ServiceProvider?.GetService(typeof(SVsSolutionPersistence)) is IVsSolutionPersistence persistence)
            {
                persistence.LoadPackageUserOpts(this, c_settingsKey);
            }
        }

        internal void OnSolutionClosed()
        {
            HasSolution = false;
            _ignoreEditorConfigGuidelines = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IgnoreEditorConfigGuidelines)));
        }

        #region IVsPersistSolutionOpts

        public int SaveUserOptions(IVsSolutionPersistence pPersistence)
        {
            // Not used; we save via SavePackageUserOpts
            return VSConstants.S_OK;
        }

        public int LoadUserOptions(IVsSolutionPersistence pPersistence, uint grfLoadOpts)
        {
            // Not used directly
            return VSConstants.S_OK;
        }

        public int WriteUserOptions(Microsoft.VisualStudio.OLE.Interop.IStream pOptionsStream, string pszKey)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pszKey != c_settingsKey)
            {
                return VSConstants.S_OK;
            }

            var data = Encoding.UTF8.GetBytes(
                c_ignoreEditorConfigKey + "=" + (_ignoreEditorConfigGuidelines ? "1" : "0") + "\n"
            );

            pOptionsStream.Write(data, (uint)data.Length, out _);
            return VSConstants.S_OK;
        }

        public int ReadUserOptions(Microsoft.VisualStudio.OLE.Interop.IStream pOptionsStream, string pszKey)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pszKey != c_settingsKey)
            {
                return VSConstants.S_OK;
            }

            // Read all bytes from the stream
            var buffer = new byte[1024];
            pOptionsStream.Read(buffer, (uint)buffer.Length, out var bytesRead);

            if (bytesRead == 0)
            {
                return VSConstants.S_OK;
            }

            var text = Encoding.UTF8.GetString(buffer, 0, (int)bytesRead);
            foreach (var line in text.Split('\n'))
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == c_ignoreEditorConfigKey)
                {
                    _ignoreEditorConfigGuidelines = parts[1].Trim() == "1";
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IgnoreEditorConfigGuidelines)));
                }
            }

            return VSConstants.S_OK;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Listens for solution open/close events.
        /// </summary>
        private sealed class SolutionEventsListener : IVsSolutionEvents
        {
            private readonly SolutionSettings _owner;

            public SolutionEventsListener(SolutionSettings owner) => _owner = owner;

            public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _owner.OnSolutionOpened();
                return VSConstants.S_OK;
            }

            public int OnAfterCloseSolution(object pUnkReserved)
            {
                _owner.OnSolutionClosed();
                return VSConstants.S_OK;
            }

            public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;
            public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
            public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;
            public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;
            public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;
            public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
            public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
            public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;
        }
    }
}
