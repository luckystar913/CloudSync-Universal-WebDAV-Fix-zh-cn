namespace CloudSync.Sync;

internal class SyncResult
{
	public int FilesUploaded { get; set; }

	public int FilesDownloaded { get; set; }

	public bool HadConflict { get; set; }

	public bool Failed { get; set; }

	public string Message { get; set; } = "";
}
