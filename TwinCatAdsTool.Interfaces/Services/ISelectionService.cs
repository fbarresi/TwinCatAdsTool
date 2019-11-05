using System;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface ISelectionService<T>
    {
        IObservable<T> Elements { get;}
        void Select(T element);
    }
}