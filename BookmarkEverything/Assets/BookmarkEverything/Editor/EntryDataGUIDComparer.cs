using System.Collections.Generic;
namespace BookmarkEverything
{
    public class EntryDataGUIDComparer : EqualityComparer<BookmarkEverythingEditor.EntryData>
    {
        public override bool Equals(BookmarkEverythingEditor.EntryData x, BookmarkEverythingEditor.EntryData y)
        {
            return x.GUID == y.GUID;
        }

        public override int GetHashCode(BookmarkEverythingEditor.EntryData obj)
        {
            throw new System.NotImplementedException();
        }
    }
}
