using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared.Extensions
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> self, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                self.Add(item);
            }
        }

        public static bool TryFindIndex<T>(this ObservableCollection<T> self, Predicate<T> predicate, out int index)
        {
            index = -1;
            var item = self.FirstOrDefault(x => predicate(x));

            if (item is null)
            {
                return false;
            }

            index = self.IndexOf(item);
            return index > -1;
        }

        public static void RemoveAll<T>(this ObservableCollection<T> self, Predicate<T> predicate)
        {
            var items = self
                .Where(x => predicate(x))
                .ToArray();

            foreach (var item in items)
            {
                self.Remove(item);
            }
        }
    }
}
