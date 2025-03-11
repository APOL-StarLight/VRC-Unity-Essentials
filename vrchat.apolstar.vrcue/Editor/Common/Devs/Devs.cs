
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace APOLStar.VRCUE.Common.Devs {
	public class DevProfile {

		public string name;
		public string twitter;
		public string discord;
		public string gumroad;

		public DevProfile(string name, string twitter, string discord, string gumroad) {
			this.name = name;
			this.twitter = twitter;
			this.discord = discord;
			this.gumroad = gumroad;
		}

		private static DevProfile[] profiles;

		public static DevProfile Get(string name) {

			if (profiles == null) {
				profiles = new DevProfile[] {
					new DevProfile("Star", "https://twitter.com/StarLight_Olls", "https://discord.gg/Ybz5DxFPVJ", "https://starlightdev.gumroad.com/"),
					new DevProfile("APOL Assets", "https://twitter.com/ApolAssets", "https://discord.gg/eCAkj4ug7p", "https://apolassets.gumroad.com/"),
				};
			}

			foreach (DevProfile profile in profiles) {
				if (profile.name == name) {
					return profile;
				}
			}
			return null;
		}
	}
}
#endif