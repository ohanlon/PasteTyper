//------------------------------------------------------------------------------
// <copyright file="TyperCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace PasteTyper
{
  /// <summary>
  /// Command handler
  /// </summary>
  internal sealed class TyperCommand
  {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x0100;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid("39b916a5-c6c3-4e45-9aab-32462b7957e7");

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly Package package;

    /// <summary>
    /// Initializes a new instance of the <see cref="TyperCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    private TyperCommand(Package package)
    {
      if (package == null)
      {
        throw new ArgumentNullException("package");
      }

      this.package = package;

      OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
      if (commandService != null)
      {
        var menuCommandId = new CommandID(CommandSet, CommandId);
        var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
        commandService.AddCommand(menuItem);
      }
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static TyperCommand Instance
    {
      get;
      private set;
    }

    /// <summary>
    /// Gets the service provider from the owner package.
    /// </summary>
    private IServiceProvider ServiceProvider => package;

    /// <summary>
    /// Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static void Initialize(Package package)
    {
      Instance = new TyperCommand(package);
    }

    private readonly List<string> clipboard = new List<string>();

    private void FillClipboard()
    {
      if (clipboard.Count != 0) return;
      if (!Clipboard.ContainsText()) return;
      string clipText = Clipboard.GetText();
      string[] clipItems = clipText.Split(new[] {"$","$"}, StringSplitOptions.RemoveEmptyEntries);

      clipboard.AddRange(clipItems);
    }

    /// <summary>
    /// This function is the callback used to execute the command when the menu item is clicked.
    /// See the constructor to see how the menu item is associated with this function using
    /// OleMenuCommandService service and MenuCommand class.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event args.</param>
    private void MenuItemCallback(object sender, EventArgs e)
    {
      FillClipboard();

      DTE dte = TyperCommandPackage.Provider;
      if (dte.ActiveDocument == null) return;

      if (dte.UndoContext.IsOpen)
        dte.UndoContext.Close();

      string textToInsert = clipboard.First();
      clipboard.RemoveAt(0);
      Random random = new Random();
      try
      {
        dte.UndoContext.Open("Pluralsight course");

        GetValue(textToInsert, random, dte);
      }
      finally
      {
        dte.UndoContext.Close();
      }
    }

    private static async void GetValue(string textToInsert, Random random, _DTE dte)
    {
      foreach (char text in from text in textToInsert select text)
      {
        int pause = random.Next(60, 110);
        string textToInsert1 = text.ToString();

        if (textToInsert1 == " " || textToInsert1 == "\t") pause = random.Next(10, 30);

        await GetValue(dte, textToInsert1, pause);
      }
    }

    private static async Task GetValue(_DTE dte, string textToInsert1, int pause)
    {
      await Task.Delay(pause);

      TextSelection sel = (TextSelection)dte.ActiveDocument.Selection;
      EditPoint startPoint = sel.TopPoint.CreateEditPoint();
      EditPoint endPoint = sel.BottomPoint.CreateEditPoint();

      sel.MoveToPoint(startPoint);
      sel.MoveToPoint(endPoint, true);

      if (sel.Text.Length == 0)
      {
        endPoint.Insert(textToInsert1);
      }
      else
      {
        endPoint.ReplaceText(startPoint, textToInsert1, (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
      }
    }
  }
}
