using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Gui.ViewModels;

namespace TwinCatAdsTool.Gui.Views
{
    public class SymbolObservationTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate IntTemplate { get; set; }
        public DataTemplate SingleTemplate { get; set; }
        public DataTemplate DoubleTemplate { get; set; }
        public DataTemplate ByteTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate DateTimeTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate LTimeSpanTemplate { get; set; }
        public DataTemplate TimeSpanTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SymbolObservationViewModel<int>)
            {
                return IntTemplate;
            }

            if (item is SymbolObservationViewModel<string>)
            {
                return StringTemplate;
            }

            if (item is SymbolObservationViewModel<float>)
            {
                return SingleTemplate;
            }

            if (item is SymbolObservationViewModel<double>)
            {
                return DoubleTemplate;
            }

            if(item is SymbolObservationViewModel<byte>)
            {
                return ByteTemplate;
            }

            if (item is SymbolObservationViewModel<DT>)
            {
                return DateTimeTemplate;
            }

            if (item is SymbolObservationViewModel<LTIME>)
            {
                return LTimeSpanTemplate;
            }
            
            if (item is SymbolObservationViewModel<TIME>)
            {
                return TimeSpanTemplate;
            }

            if (item is SymbolObservationViewModel<bool>)
            {
                return BoolTemplate;
            }

            if (item is SymbolObservationViewModel<ushort>)
            {
                return IntTemplate;
            }

            if (item is SymbolObservationViewModel<uint>)
            {
                return IntTemplate;
            }

            if (item is SymbolObservationViewModel<sbyte>)
            {
                return ByteTemplate;
            }

            if (item is SymbolObservationViewModel<short>)
            {
                return IntTemplate;
            }

            if (item is SymbolObservationViewModel<long>)
            {
                return IntTemplate;
            }

            if (item is SymbolObservationViewModel<ulong>)
            {
                return IntTemplate;
            }

            return DefaultTemplate;
        }
    }
}