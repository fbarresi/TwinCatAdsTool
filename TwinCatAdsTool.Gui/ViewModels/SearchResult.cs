using System;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.TypeSystem;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public struct SearchResult : IEquatable<SearchResult>
    {
        public string SearchTerm { get; set; }
        public IEnumerable<ISymbol> Results { get; set; }

        public bool Equals(SearchResult other)
        {
            return string.Equals(SearchTerm, other.SearchTerm) && Equals(Results, other.Results);
        }

        public override bool Equals(object obj)
        {
            return obj is SearchResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SearchTerm != null ? SearchTerm.GetHashCode() : 0) * 397) ^ (Results != null ? Results.GetHashCode() : 0);
            }
        }

        public static bool operator ==(SearchResult left, SearchResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SearchResult left, SearchResult right)
        {
            return !left.Equals(right);
        }
    }
}