// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Octothorpe.Mac
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		AppKit.NSPopUpButton FileSelection { get; set; }

		[Outlet]
		AppKit.NSBox NSBoxModel { get; set; }

		[Outlet]
		AppKit.NSButton NSButtonCheckBeta { get; set; }

		[Outlet]
		AppKit.NSButtonCell NSButtonFullTable { get; set; }

		[Outlet]
		AppKit.NSButton NSButtonParse { get; set; }

		[Outlet]
		AppKit.NSButton NSButtonRemoveStubs { get; set; }

		[Outlet]
		AppKit.NSTextField NSTextFieldDevice { get; set; }

		[Outlet]
		AppKit.NSTextField NSTextFieldLoc { get; set; }

		[Outlet]
		AppKit.NSTextField NSTextFieldMax { get; set; }

		[Outlet]
		AppKit.NSTextField NSTextFieldMin { get; set; }

		[Outlet]
		AppKit.NSTextField NSTextFieldModel { get; set; }

		[Outlet]
		AppKit.NSTextView NSTextViewOutput { get; set; }

		[Action ("ChangeOutputFormat:")]
		partial void ChangeOutputFormat (AppKit.NSButton sender);

		[Action ("ParsingSTART:")]
		partial void ParsingSTART (AppKit.NSButton sender);

		[Action ("SourceChanged:")]
		partial void SourceChanged (AppKit.NSPopUpButton sender);

		[Action ("SourceEdited:")]
		partial void SourceEdited (AppKit.NSTextField sender);

		[Action ("ToggleModelField:")]
		partial void ToggleModelField (AppKit.NSTextField sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (FileSelection != null) {
				FileSelection.Dispose ();
				FileSelection = null;
			}

			if (NSBoxModel != null) {
				NSBoxModel.Dispose ();
				NSBoxModel = null;
			}

			if (NSButtonCheckBeta != null) {
				NSButtonCheckBeta.Dispose ();
				NSButtonCheckBeta = null;
			}

			if (NSButtonFullTable != null) {
				NSButtonFullTable.Dispose ();
				NSButtonFullTable = null;
			}

			if (NSButtonParse != null) {
				NSButtonParse.Dispose ();
				NSButtonParse = null;
			}

			if (NSButtonRemoveStubs != null) {
				NSButtonRemoveStubs.Dispose ();
				NSButtonRemoveStubs = null;
			}

			if (NSTextFieldDevice != null) {
				NSTextFieldDevice.Dispose ();
				NSTextFieldDevice = null;
			}

			if (NSTextFieldLoc != null) {
				NSTextFieldLoc.Dispose ();
				NSTextFieldLoc = null;
			}

			if (NSTextFieldMax != null) {
				NSTextFieldMax.Dispose ();
				NSTextFieldMax = null;
			}

			if (NSTextFieldMin != null) {
				NSTextFieldMin.Dispose ();
				NSTextFieldMin = null;
			}

			if (NSTextFieldModel != null) {
				NSTextFieldModel.Dispose ();
				NSTextFieldModel = null;
			}

			if (NSTextViewOutput != null) {
				NSTextViewOutput.Dispose ();
				NSTextViewOutput = null;
			}
		}
	}
}
