namespace CarePlusPharmacy.Models.ViewModels
{
    // Field types supported by the shared _FilterBar partial.
    public enum FilterFieldType
    {
        Text,
        Select,
        Date
    }

    // A single filter control rendered by Views/Shared/_FilterBar.cshtml.
    // Name is the query-string (and form field) name; Value is the currently
    // applied value; Options feed <select> fields.
    public class FilterField
    {
        public string Name { get; set; } = string.Empty;
        public string? Label { get; set; }
        public FilterFieldType Type { get; set; } = FilterFieldType.Text;
        public string? Value { get; set; }
        public List<FilterOption> Options { get; set; } = new();
    }

    public class FilterOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}