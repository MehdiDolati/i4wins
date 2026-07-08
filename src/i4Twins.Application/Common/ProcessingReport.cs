namespace i4Twins.Application.DTOs;

public class ProcessingReport
{
    public int TotalLines { get; set; }
    public int StoredCount { get; set; }
    public int DuplicatesSkipped { get; set; }
    public int InvalidRecords { get; set; }

    public override string ToString()
    {
        return $"Total: {TotalLines}, Stored: {StoredCount}, Duplicates: {DuplicatesSkipped}, Invalid: {InvalidRecords}";
    }
}