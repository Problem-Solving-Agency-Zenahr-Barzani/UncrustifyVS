using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Generic;

namespace UncrustifyVs
{
    /// <summary>
    /// The extension package (package entry point).
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.1.3", IconResourceID = 400)] // Used in the Help > About dialog.
    [ProvideMenuResource("Menus.ctmenu", 1)] // Let's the shell know this package exposes menus.
    [ProvideOptionPage(typeof(OptionsPageGeneral), "UncrustifyVS", "General", 101, 111, true)]
    [ProvideLoadKey("Standard", "1.1", "UncrustifyVS", "Gokhan Ozdogan", 104)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [Guid(GuidStrings.Package)]
    public sealed class UncrustifyVsPackage : Package
    {
        /// <summary>
        /// Maps file extensions to Uncrustify language strings.
        /// </summary>
        private static Dictionary<string, KeyValuePair<LanguageFilters, string>> _extToUncrustifyLanguage = new Dictionary<string, KeyValuePair<LanguageFilters, string>>();
        /// <summary>
        /// The DTE.
        /// </summary>
        private DTE _dte;
        /// <summary>
        /// Cached DTE events.
        /// </summary>
        /// <remarks>A reference to this must be held so that bound callbacks don't get GC'd.</remarks>
        private Events _dteEvents;
        /// <summary>
        /// Cached DTE document events.
        /// </summary>
        /// <remarks>A reference to this must be held so that bound callbacks don't get GC'd.</remarks>
        private DocumentEvents _dteDocEvents;
        /// <summary>
        /// The options page for the extension.
        /// </summary>
        private OptionsPageGeneral _generalOptions;
        /// <summary>
        /// If set, ignores the next save operation.
        /// </summary>
        private bool _ignoreNextSave;
        /// <inheritdoc />
        public UncrustifyVsPackage()
        {
            MapExtension("c", LanguageFilters.Cpp);
            MapExtension("cpp", LanguageFilters.Cpp);
            MapExtension("h", LanguageFilters.Cpp);

            MapExtension("cs", LanguageFilters.Cs);

            MapExtension("d", LanguageFilters.D);

            MapExtension("java", LanguageFilters.Java);
        }
        /// <summary>
        /// Maps an extension to a target language.
        /// </summary>
        /// <param name="ext">The extension without a leading period.</param>
        /// <param name="language">The language.</param>
        private void MapExtension(string ext, LanguageFilters language)
        {
            ext = "." + ext;
            switch (language)
            {
                case LanguageFilters.Cpp:
                    _extToUncrustifyLanguage.Add(ext, new KeyValuePair<LanguageFilters, string>(language, "CPP"));
                    break;
                case LanguageFilters.Cs:
                    _extToUncrustifyLanguage.Add(ext, new KeyValuePair<LanguageFilters, string>(language, "CSharp"));
                    break;
                case LanguageFilters.D:
                    _extToUncrustifyLanguage.Add(ext, new KeyValuePair<LanguageFilters, string>(language, "D"));
                    break;
                case LanguageFilters.Java:
                    _extToUncrustifyLanguage.Add(ext, new KeyValuePair<LanguageFilters, string>(language, "JAVA"));
                    break;
                case LanguageFilters.All:
                    _extToUncrustifyLanguage.Add(ext, new KeyValuePair<LanguageFilters, string>(language, "OTHER"));
                    break;
            }
        }
        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            if (InitializeEnvironment())
            {
                AddMenu();
            }
        }
        /// <summary>
        /// Formats a document when the "format on save" flag is set.
        /// </summary>
        /// <param name="document">The document to format.</param>
        private void DocEvents_Saved(Document document)
        {
            if (!_ignoreNextSave && _generalOptions.FormatOnDocSaved)
            {
                if (UncrustifyDocument(document))
                {
                    _ignoreNextSave = true;
                    try
                    {
                        document.Save();
                    }
                    finally
                    {
                        _ignoreNextSave = false;
                    }
                }
            }
        }
        /// <summary>
        /// Formats a document when the "format on open" flag is set.
        /// </summary>
        /// <param name="document">The document to format.</param>
        private void DocEvents_Opened(Document document)
        {
            if (_generalOptions.FormatOnDocOpened)
            {
                UncrustifyDocument(document);
            }
        }
        /// <summary>
        /// Initializes the package environment by hooking into the VS IDE.
        /// </summary>
        /// <returns>If the environment was successfully initialized, the return value is true. Otherwise, the return value is false.</returns>
        private bool InitializeEnvironment()
        {
            try
            {
                // Cache the environment
                _dte = GetService(typeof(DTE)) as DTE;
                if (_dte == null)
                {
                    return false;
                }

                // Cache the options page.
                _generalOptions = GetAutomationObject("UncrustifyVS.General") as OptionsPageGeneral;
                if (_generalOptions == null)
                {
                    return false;
                }

                // Save the environment events so they don't get GC'd.
                _dteEvents = _dte.Events;
                _dteDocEvents = _dteEvents.DocumentEvents;

                // Register the document callbacks for event-driven formatting
                _dteDocEvents.DocumentSaved += DocEvents_Saved;
                _dteDocEvents.DocumentOpened += DocEvents_Opened;

                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Adds the add-in to the Tools menu.
        /// </summary>
        private void AddMenu()
        {
            var menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (menuCommandService != null)
            {
                menuCommandService.AddCommand(MenuCommand(CommandIds.UncrustifyDocument, OnUncrustifyDocument, BeforeOnUncrustifyDocument));
                menuCommandService.AddCommand(MenuCommand(CommandIds.UncrustifySelection, OnUncrustifySelection, BeforeOnUncrustifySelection));
                menuCommandService.AddCommand(MenuCommand(CommandIds.Options, OnShowOptions));
            }
        }
        /// <summary>
        /// Adds a menu command to the package menu.
        /// </summary>
        /// <param name="commandService">The menu command service.</param>
        /// <param name="id">The command ID.</param>
        /// <param name="executeHandler">The command handler.</param>
        /// <param name="canExecuteHandler">The command status handler.</param>
        private OleMenuCommand MenuCommand(CommandIds id, EventHandler executeHandler, EventHandler canExecuteHandler = null)
        {
            var menuItem = new OleMenuCommand(executeHandler, new CommandID(Guids.PackageCommandSet, (int)id));
            if (canExecuteHandler != null)
            {
                menuItem.BeforeQueryStatus += canExecuteHandler;
            }
            return menuItem;
        }
        /// <summary>
        /// Shows the Options page.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Information about the event.</param>
        private void OnShowOptions(object sender, EventArgs e) => ShowOptionPage(typeof(OptionsPageGeneral));
        /// <summary>
        /// Returns whether a given document can be formatted.
        /// </summary>
        /// <remarks>This function must be run before a document is formatted.</remarks>
        /// <param name="document">The document to format.</param>
        /// <param name="uncrustifyLanguage">The Uncrustify language string for the document.</param>
        /// <returns>True, if the document can be formatted. Otherwise false.</returns>
        private bool CanFormatDocument(Document document, out string uncrustifyLanguage)
        {
            uncrustifyLanguage = null;

            // We need an active text document, which is not marked as readonly.
            if (document == null || document.ReadOnly || document.Type != "Text")
            {
                return false;
            }

            // See if the language filter fits the file extension.
            var ext = Path.GetExtension(document.FullName).ToLowerInvariant();
            if (!_extToUncrustifyLanguage.TryGetValue(ext, out KeyValuePair<LanguageFilters, string> kv))
            {
                return false;
            }

            var result = _generalOptions.LanguageFilter == LanguageFilters.All || kv.Key == _generalOptions.LanguageFilter;
            if (result)
            {
                uncrustifyLanguage = kv.Value;
            }

            return result;
        }
        /// <summary>
        /// Returns the state of the "format document" menu command.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Information about the event.</param>
        private void BeforeOnUncrustifyDocument(object sender, EventArgs e)
        {
            if (sender != null)
            {
                var item = (OleMenuCommand)sender;
                string unused;
                item.Enabled = CanFormatDocument(_dte.ActiveDocument, out unused);
            }
        }
        /// <summary>
        /// Returns the state of the "format selection" menu command.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Unused.</param>
        private void BeforeOnUncrustifySelection(object sender, EventArgs e)
        {
            var item = (OleMenuCommand)sender;
            var txtDoc = _dte.ActiveDocument?.Object("TextDocument") as TextDocument;
            string unused;
            item.Enabled = txtDoc != null && CanFormatDocument(_dte.ActiveDocument, out unused) && !txtDoc.Selection.IsEmpty;
        }
        /// <summary>
        /// Formats the active document using Uncrustify.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Unused.</param>
        private void OnUncrustifyDocument(object sender, EventArgs e) => UncrustifyDocument(_dte.ActiveDocument);
        /// <summary>
        /// Formats the current selection using Uncrustify.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Information about the event.</param>
        private void OnUncrustifySelection(object sender, EventArgs e) => UncrustifyDocument(_dte.ActiveDocument, true);
        /// <summary>
        /// Formats the active document using Uncrustify.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="selectionOnly">If set, formats the active selection only. Otherwise the whole document is formatted.</param>
        private bool UncrustifyDocument(Document document, bool selectionOnly = false)
        {
            string uncrustifyLanguage;
            if (!CanFormatDocument(document, out uncrustifyLanguage))
            {
                return false;
            }

            string tmpFilePath = null;
            try
            {
                var txtDoc = (TextDocument)document.Object("TextDocument");

                // Push some information about the current view
                var oldSelectedLine = txtDoc.Selection.ActivePoint.Line;
                var oldSelectedColumn = txtDoc.Selection.ActivePoint.LineCharOffset;
                var oldLineLength = txtDoc.Selection.ActivePoint.LineLength;
                var txtPane = txtDoc.Selection.TextPane;
                var oldTopLine = txtPane.Width > -1 ? txtPane.StartPoint.Line : oldSelectedLine;

                // Create temporary file and dump current document into it
                var startPt = (selectionOnly ? txtDoc.Selection.AnchorPoint : txtDoc.StartPoint).CreateEditPoint();
                var endPt = (selectionOnly ? txtDoc.Selection.ActivePoint : txtDoc.EndPoint).CreateEditPoint();
                var caretOffset = CaretOffset(txtDoc);
                var docTxt = startPt.GetText(endPt);
                tmpFilePath = Path.GetTempFileName();
                File.WriteAllText(tmpFilePath, docTxt);

                // Run Uncrustify in background mode
                _dte.StatusBar.Text = "Formatting document. Please wait...";
                _dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationGeneral);
                _dte.StatusBar.Highlight(true);

                var fmtCmdLine = _generalOptions.CommandLine.Replace("%LANGUAGE%", uncrustifyLanguage).
                                                             Replace("%CFGFILE%", _generalOptions.CfgFilePath).
                                                             Replace("%FILE%", tmpFilePath).
                                                             Replace("%FILENAME%", document.Name).
                                                             Replace("%FILE_DIR%", document.Path).
                                                             Replace("%SOLUTION%", _dte.Solution.FullName);

                // NOTE(gokhan.ozdogan): we are ignoring the profiles flag here. Always do this.
                // Add --frag to command line
                if (selectionOnly)
                {
                    fmtCmdLine += " --frag";
                }

                var psi = new ProcessStartInfo(_generalOptions.ProgramFilePath, fmtCmdLine)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process.Start())
                    {
                        // Wait for Uncrustify to finish processing
                        process.WaitForExit();

                        // Load the formatted text back into memory.
                        var fmtTxt = File.ReadAllText(tmpFilePath);

                        // And replace the text in the active document.
                        startPt.ReplaceText(endPt, fmtTxt, (int)vsEPReplaceTextOptions.vsEPReplaceTextNormalizeNewlines | (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);

                        if (docTxt != fmtTxt)
                        {
                            try
                            {
                                // Try to restore the view.
                                txtDoc.Selection.GotoLine(oldTopLine);
                                txtPane.TryToShow(txtDoc.Selection.ActivePoint, vsPaneShowHow.vsPaneShowTop);

                                // Try to restore the selection.
                                var restoredSelectionPoint = RestoreCaretOffset(txtDoc, caretOffset);
                                if (restoredSelectionPoint != null)
                                {
                                    txtDoc.Selection.MoveToPoint(restoredSelectionPoint);
                                }
                                else
                                {
                                    // Restore using line and column.
                                    txtDoc.Selection.MoveToLineAndOffset(oldSelectedLine, oldSelectedColumn);

                                    // Try to account for a shortened line.
                                    var newLineLength = txtDoc.Selection.ActivePoint.LineLength;
                                    var dtLineLength = newLineLength - oldLineLength;
                                    if (dtLineLength < 0 && oldSelectedColumn > newLineLength)
                                    {
                                        txtDoc.Selection.MoveToLineAndOffset(oldSelectedLine, oldSelectedColumn + dtLineLength);
                                    }
                                }
                            }
                            catch
                            {
                                // Nothing to do.
                            }
                        }

                        _dte.StatusBar.Text = "Document was successfully formatted.";
                        return true;
                    }
                    else
                    {
                        _dte.StatusBar.Text = "Could not launch Uncrustify.";
                    }
                }
            }
            catch (Exception e)
            {
                _dte.StatusBar.Text = $"Could not format the active document: {e.Message}";
            }
            finally
            {
                try
                {
                    if (tmpFilePath != null)
                    {
                        File.Delete(tmpFilePath);
                    }
                }
                catch
                {
                    // Nothing to do.
                }

                _dte.StatusBar.Highlight(false);
                _dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationGeneral);
            }

            return false;
        }
        /// <summary>
        /// Creates a caret offset that can later be restored if only non-character symbols change.
        /// </summary>
        /// <param name="document">The text document.</param>
        /// <returns>The caret offset.</returns>
        public string CaretOffset(TextDocument document)
        {
            var prefixText = document.StartPoint.CreateEditPoint().GetText(document.Selection.ActivePoint);
            var result = string.Empty;
            var trailingWildcards = 0;
            foreach (var c in prefixText)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (c != '\r')
                    {
                        ++trailingWildcards;
                    }
                }
                else if (!char.IsControl(c))
                {
                    result += c;
                    trailingWildcards = 0;
                }
            }

            result += new string(' ', trailingWildcards);
            return result;
        }
        /// <summary>
        /// Restores a caret offset that was previously created with <see cref="CaretOffset"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <remarks>The reconstruction is not perfect - improve this in the future.</remarks>
        public EditPoint RestoreCaretOffset(TextDocument document, string offset)
        {
            var caretPoint = document.StartPoint.CreateEditPoint();
            if (string.IsNullOrEmpty(offset))
            {
                return caretPoint;
            }

            var text = caretPoint.GetText(int.MaxValue);
            var absoluteOffset = 1;
            var i = 0;
            for (var j = 0; i < text.Length && j < offset.Length; ++i)
            {
                var a = text[i];
                var b = offset[j];
                if (a == b)
                {
                    ++absoluteOffset;
                    ++j;
                }
                else if (a == ' ' || a == '\n' || a == '\t')
                {
                    ++absoluteOffset;

                    // Consume wildcard.
                    if (b == ' ')
                    {
                        ++j;
                    }
                }
                else if (a != '\r')
                {
                    if (b == ' ')
                    {
                        break;
                    }
                    else
                    {
                        // The specified offset is not valid for the document.
                        return null;
                    }
                }
            }

            if (i > 0 && text[i - 1] == '\n')
            {
                --absoluteOffset;
            }

            caretPoint.MoveToAbsoluteOffset(absoluteOffset);
            return caretPoint;
        }
    }
}