using System;
using Newtonsoft.Json;

namespace RustServerManager.Models
{
    public partial class UmodInfoResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("game_url")]
        public Uri GameUrl { get; set; }

        [JsonProperty("snapshot_url")]
        public Uri SnapshotUrl { get; set; }

        [JsonProperty("icon_url")]
        public Uri IconUrl { get; set; }

        [JsonProperty("repository")]
        public string Repository { get; set; }

        [JsonProperty("server_appid")]
        public long ServerAppid { get; set; }

        [JsonProperty("client_appid")]
        public long ClientAppid { get; set; }

        [JsonProperty("buildable")]
        public long Buildable { get; set; }

        [JsonProperty("installation_paths")]
        public string InstallationPaths { get; set; }

        [JsonProperty("target_framework")]
        public string TargetFramework { get; set; }

        [JsonProperty("public_branch_name")]
        public object PublicBranchName { get; set; }

        [JsonProperty("public_branch_description")]
        public object PublicBranchDescription { get; set; }

        [JsonProperty("preprocessor_symbol")]
        public string PreprocessorSymbol { get; set; }

        [JsonProperty("steam_authenticated")]
        public long SteamAuthenticated { get; set; }

        [JsonProperty("download_url")]
        public Uri DownloadUrl { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("plugin_count")]
        public long PluginCount { get; set; }

        [JsonProperty("extension_count")]
        public long ExtensionCount { get; set; }

        [JsonProperty("product_count")]
        public long ProductCount { get; set; }

        [JsonProperty("steam_branches")]
        public SteamBranch[] SteamBranches { get; set; }

        [JsonProperty("latest_release_version")]
        public string LatestReleaseVersion { get; set; }

        [JsonProperty("latest_release_version_formatted")]
        public string LatestReleaseVersionFormatted { get; set; }

        [JsonProperty("latest_release_version_checksum")]
        public string LatestReleaseVersionChecksum { get; set; }

        [JsonProperty("latest_release_at")]
        public DateTimeOffset LatestReleaseAt { get; set; }

        [JsonProperty("latest_release_at_atom")]
        public DateTimeOffset LatestReleaseAtAtom { get; set; }

        [JsonProperty("watchers")]
        public long Watchers { get; set; }

        [JsonProperty("watchers_shortened")]
        public string WatchersShortened { get; set; }
    }

    public partial class SteamBranch
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pwdrequired")]
        public long Pwdrequired { get; set; }

        [JsonProperty("timeupdated")]
        public long? Timeupdated { get; set; }

        [JsonProperty("buildid")]
        public long Buildid { get; set; }
    }
}
