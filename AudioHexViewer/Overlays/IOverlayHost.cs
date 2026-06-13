using System.Collections.Generic;
using System.Drawing;

namespace AudioHexViewer
{
    /// <summary>
    /// Contrat minimal exposé par un contrôle "hôte" d'overlays (SpectrumControl, OscilloscopeControl, ...)
    /// pour permettre à OverlaysForm de fonctionner avec n'importe lequel d'entre eux.
    /// </summary>
    internal interface IOverlayHost
    {
        // Liste / état
        List<OverlayEntry> OverlayLibraryGetEntries();
        bool OverlayLibraryGetVisible(string libraryFilePath);
        void OverlayLibrarySetVisible(string filePath, bool visible);

        // Capture (dépend du Kind propre à l'hôte)
        OverlayEntry OverlayLibraryCaptureActiveView();

        // Import / Export / Suppression / Renommage / Masquage
        OverlayEntry OverlayLibraryImport(string sourcePath);
        void OverlayLibraryExport(string libraryFilePath, string targetPath);
        void OverlayLibraryDelete(string libraryFilePath);
        OverlayEntry OverlayLibraryRename(string libraryFilePath, string newName);
        void OverlayLibraryRemoveFromList(string libraryFilePath);
        void OverlayLibraryUnhideFromList(string libraryFilePath);

        // Divers
        string OverlayLibraryGetFolderPath();
        Color OverlayGetSlotColor1Based(int slotNumber);
    }
}
