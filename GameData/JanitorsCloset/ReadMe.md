Janitor's Closet

Description
This mod will allow you to block parts from being displayed in the Editor, either on a soft basis or hard basis.  The difference is that a soft basis doesn't do anything to the parts, it merely blocks the parts from being displayed.  In this case, the parts are still loaded.  This would be useful in an ongoing career, where you have ships with parts you don't use anymore, but are still active.
A hard basis is where the files get renamed so that none of the parts or their assets will be loaded.  This can speed up loading a game, but is potentially game-breaking, in that if you prune a part and it is in use on a vessel, the vessel will be deleted the next time you start the game.
There is also the ability to export a list of all blocked parts, so that you can either import it into another game or send it to someone else.

Instructions

In the editor (VAB or SPH), hold the Alt key while clicking on a part.  A two line menu will be shown, giving you the option to block the part in the scene you are currently in (VAB or SPH), or block it entirely.  This does the soft blocking, where it isn't changed, but just blocked from displaying.
There is a toolbar button, of a broom & dustbin.  Hovering the mouse over it will display a popup menu:

ShowBlocked
Unblock
PermaPrune
Export/Import

ShowBlocked will display a list of all parts blocked, and, if a soft basis, a button so you can unblock a single part
Unblock will unblock all parts which are blocked via the soft basis
PermaPrune will display a new menu with the following:
	Permanent Prune
	Undo Permanent Prune
	Cancel

	Permanent Prune will permanently rename files to prevent them from being loaded.  It will only do this to parts which are blocked everywhere.
	Undo Permanent Prune will undo all the permanently pruned files

Import/Export will display a new menu with the following:
	Export
	Import
	Cancel

	Export will present a dialog where you can name the list of pruned parts you are exporting.  Export lists are always saved to the GameData/JanitorsCloset/PluginData directory, with a suffix of ".prnlst", and the full path is stored in the system clipboard after the export.
	Import will present a file dialog where you can select a file for importation.

Known Issues
Clickthrough is a problem with the file selection dialog.  It starts in the PluginData directory, but you can go anywhere to find a file.  Again, it only displays files which have the suffix ".prnlst".


TODO
Listbox showing files renamed ???
