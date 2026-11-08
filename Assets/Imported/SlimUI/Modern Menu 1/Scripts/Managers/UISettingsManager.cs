using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

namespace SlimUI.ModernMenu{
	public class UISettingsManager : MonoBehaviour {

		public enum Platform {Desktop, Mobile};
		public Platform platform;
		// toggle buttons
		[Header("MOBILE SETTINGS")]
		public GameObject mobileSFXtext;
		public GameObject mobileMusictext;
		public GameObject mobileShadowofftextLINE;
		public GameObject mobileShadowlowtextLINE;
		public GameObject mobileShadowhightextLINE;

		[Header("VIDEO SETTINGS")]
		public GameObject fullscreentext;
		public GameObject ambientocclusiontext;
		public GameObject shadowofftextLINE;
		public GameObject shadowlowtextLINE;
		public GameObject shadowhightextLINE;
		public GameObject aaofftextLINE;
		public GameObject aa2xtextLINE;
		public GameObject aa4xtextLINE;
		public GameObject aa8xtextLINE;
		public GameObject vsynctext;
		public GameObject motionblurtext;
		public GameObject texturelowtextLINE;
		public GameObject texturemedtextLINE;
		public GameObject texturehightextLINE;
		public GameObject cameraeffectstext; 

		[Header("GAME SETTINGS")]
		public GameObject showhudtext;
		public GameObject tooltipstext;
		public GameObject difficultynormaltext;
		public GameObject difficultynormaltextLINE;
		public GameObject difficultyhardcoretext;
		public GameObject difficultyhardcoretextLINE;

		[Header("CONTROLS SETTINGS")]
		public GameObject invertmousetext;

		// sliders
		public GameObject musicSlider;
		public GameObject sensitivityXSlider;
		public GameObject sensitivityYSlider;
		public GameObject mouseSmoothSlider;

		private UnityEngine.UI.Slider _music;
		private UnityEngine.UI.Slider _sensitivityX;
		private UnityEngine.UI.Slider _sensitivityY;
		private UnityEngine.UI.Slider _smoothing;

		private void Awake()
		{
			_music = musicSlider.GetComponent<UnityEngine.UI.Slider>();
			_sensitivityX = sensitivityXSlider.GetComponent<UnityEngine.UI.Slider>();
			_sensitivityY = sensitivityYSlider.GetComponent<UnityEngine.UI.Slider>();
			_smoothing = mouseSmoothSlider.GetComponent<UnityEngine.UI.Slider>();
		}

		private void OnEnable()
		{
			GameSettings.Changed -= RefreshSettings;
			GameSettings.Changed += RefreshSettings;
			RefreshSettings();
		}

		public void Start()
		{
			GameSettings.Changed -= RefreshSettings;
			GameSettings.Changed += RefreshSettings;
			RefreshSettings();
		}

		private void OnDisable()
		{
			GameSettings.Changed -= RefreshSettings;
			GameSettings.Save();
		}

		public void RefreshSettings()
		{
			_music.SetValueWithoutNotify(GameSettings.MusicVolume);
			_sensitivityX.SetValueWithoutNotify(GameSettings.HorizontalSensitivity);
			_sensitivityY.SetValueWithoutNotify(GameSettings.VerticalSensitivity);
			_smoothing.SetValueWithoutNotify(GameSettings.MouseSmoothing);
			showhudtext.GetComponent<TMPro.TMP_Text>().text = GameSettings.ShowHUD ? "on" : "off";
			tooltipstext.GetComponent<TMPro.TMP_Text>().text = GameSettings.ToolTips ? "on" : "off";
			invertmousetext.GetComponent<TMPro.TMP_Text>().text = GameSettings.InvertMouse ? "on" : "off";
		}

		public void FullScreen (){
			Screen.fullScreen = !Screen.fullScreen;

			if(Screen.fullScreen == true){
				fullscreentext.GetComponent<TMP_Text>().text = "on";
			}
			else if(Screen.fullScreen == false){
				fullscreentext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void MusicSlider () => GameSettings.SetMusicVolume(_music.value);
		public void SensitivityXSlider () => GameSettings.SetHorizontalSensitivity(_sensitivityX.value);
		public void SensitivityYSlider () => GameSettings.SetVerticalSensitivity(_sensitivityY.value);
		public void SensitivitySmoothing () => GameSettings.SetMouseSmoothing(_smoothing.value);
		public void ShowHUD () => GameSettings.SetShowHUD(!GameSettings.ShowHUD);

		// the playerprefs variable that is checked to enable mobile sfx while in game
		public void MobileSFXMute (){
			if(PlayerPrefs.GetInt("Mobile_MuteSfx")==0){
				PlayerPrefs.SetInt("Mobile_MuteSfx",1);
				mobileSFXtext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("Mobile_MuteSfx")==1){
				PlayerPrefs.SetInt("Mobile_MuteSfx",0);
				mobileSFXtext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void MobileMusicMute (){
			if(PlayerPrefs.GetInt("Mobile_MuteMusic")==0){
				PlayerPrefs.SetInt("Mobile_MuteMusic",1);
				mobileMusictext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("Mobile_MuteMusic")==1){
				PlayerPrefs.SetInt("Mobile_MuteMusic",0);
				mobileMusictext.GetComponent<TMP_Text>().text = "off";
			}
		}

		// show tool tips like: 'How to Play' control pop ups
		public void ToolTips () => GameSettings.SetToolTips(!GameSettings.ToolTips);

		public void NormalDifficulty (){
			difficultyhardcoretextLINE.gameObject.SetActive(false);
			difficultynormaltextLINE.gameObject.SetActive(true);
			PlayerPrefs.SetInt("NormalDifficulty",1);
			PlayerPrefs.SetInt("HardCoreDifficulty",0);
		}

		public void HardcoreDifficulty (){
			difficultyhardcoretextLINE.gameObject.SetActive(true);
			difficultynormaltextLINE.gameObject.SetActive(false);
			PlayerPrefs.SetInt("NormalDifficulty",0);
			PlayerPrefs.SetInt("HardCoreDifficulty",1);
		}

		public void ShadowsOff (){
			PlayerPrefs.SetInt("Shadows",0);
			QualitySettings.shadowCascades = 0;
			QualitySettings.shadowDistance = 0;
			shadowofftextLINE.gameObject.SetActive(true);
			shadowlowtextLINE.gameObject.SetActive(false);
			shadowhightextLINE.gameObject.SetActive(false);
		}

		public void ShadowsLow (){
			PlayerPrefs.SetInt("Shadows",1);
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 75;
			shadowofftextLINE.gameObject.SetActive(false);
			shadowlowtextLINE.gameObject.SetActive(true);
			shadowhightextLINE.gameObject.SetActive(false);
		}

		public void ShadowsHigh (){
			PlayerPrefs.SetInt("Shadows",2);
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 500;
			shadowofftextLINE.gameObject.SetActive(false);
			shadowlowtextLINE.gameObject.SetActive(false);
			shadowhightextLINE.gameObject.SetActive(true);
		}

		public void MobileShadowsOff (){
			PlayerPrefs.SetInt("MobileShadows",0);
			QualitySettings.shadowCascades = 0;
			QualitySettings.shadowDistance = 0;
			mobileShadowofftextLINE.gameObject.SetActive(true);
			mobileShadowlowtextLINE.gameObject.SetActive(false);
			mobileShadowhightextLINE.gameObject.SetActive(false);
		}

		public void MobileShadowsLow (){
			PlayerPrefs.SetInt("MobileShadows",1);
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 75;
			mobileShadowofftextLINE.gameObject.SetActive(false);
			mobileShadowlowtextLINE.gameObject.SetActive(true);
			mobileShadowhightextLINE.gameObject.SetActive(false);
		}

		public void MobileShadowsHigh (){
			PlayerPrefs.SetInt("MobileShadows",2);
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 500;
			mobileShadowofftextLINE.gameObject.SetActive(false);
			mobileShadowlowtextLINE.gameObject.SetActive(false);
			mobileShadowhightextLINE.gameObject.SetActive(true);
		}

		public void vsync (){
			if(QualitySettings.vSyncCount == 0){
				QualitySettings.vSyncCount = 1;
				vsynctext.GetComponent<TMP_Text>().text = "on";
			}
			else if(QualitySettings.vSyncCount == 1){
				QualitySettings.vSyncCount = 0;
				vsynctext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void InvertMouse () => GameSettings.SetInvertMouse(!GameSettings.InvertMouse);

		public void MotionBlur (){
			if(PlayerPrefs.GetInt("MotionBlur")==0){
				PlayerPrefs.SetInt("MotionBlur",1);
				motionblurtext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("MotionBlur")==1){
				PlayerPrefs.SetInt("MotionBlur",0);
				motionblurtext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void AmbientOcclusion (){
			if(PlayerPrefs.GetInt("AmbientOcclusion")==0){
				PlayerPrefs.SetInt("AmbientOcclusion",1);
				ambientocclusiontext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("AmbientOcclusion")==1){
				PlayerPrefs.SetInt("AmbientOcclusion",0);
				ambientocclusiontext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void CameraEffects (){
			if(PlayerPrefs.GetInt("CameraEffects")==0){
				PlayerPrefs.SetInt("CameraEffects",1);
				cameraeffectstext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("CameraEffects")==1){
				PlayerPrefs.SetInt("CameraEffects",0);
				cameraeffectstext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void TexturesLow (){
			PlayerPrefs.SetInt("Textures",0);
			QualitySettings.globalTextureMipmapLimit = 2;
			texturelowtextLINE.gameObject.SetActive(true);
			texturemedtextLINE.gameObject.SetActive(false);
			texturehightextLINE.gameObject.SetActive(false);
		}

		public void TexturesMed (){
			PlayerPrefs.SetInt("Textures",1);
			QualitySettings.globalTextureMipmapLimit = 1;
			texturelowtextLINE.gameObject.SetActive(false);
			texturemedtextLINE.gameObject.SetActive(true);
			texturehightextLINE.gameObject.SetActive(false);
		}

		public void TexturesHigh (){
			PlayerPrefs.SetInt("Textures",2);
			QualitySettings.globalTextureMipmapLimit = 0;
			texturelowtextLINE.gameObject.SetActive(false);
			texturemedtextLINE.gameObject.SetActive(false);
			texturehightextLINE.gameObject.SetActive(true);
		}

		public void AntiAliasingOff(){
			PlayerPrefs.SetInt("AntiAliasing", 0);
			QualitySettings.antiAliasing = 0;
			if(aaofftextLINE) aaofftextLINE.SetActive(true);
			if(aa2xtextLINE) aa2xtextLINE.SetActive(false);
			if(aa4xtextLINE) aa4xtextLINE.SetActive(false);
			if(aa8xtextLINE) aa8xtextLINE.SetActive(false);
		}

		public void AntiAliasing2X(){
			PlayerPrefs.SetInt("AntiAliasing", 2);
			QualitySettings.antiAliasing = 2;
			if(aaofftextLINE) aaofftextLINE.SetActive(false);
			if(aa2xtextLINE) aa2xtextLINE.SetActive(true);
			if(aa4xtextLINE) aa4xtextLINE.SetActive(false);
			if(aa8xtextLINE) aa8xtextLINE.SetActive(false);
		}

		public void AntiAliasing4X(){
			PlayerPrefs.SetInt("AntiAliasing", 4);
			QualitySettings.antiAliasing = 4;
			if(aaofftextLINE) aaofftextLINE.SetActive(false);
			if(aa2xtextLINE) aa2xtextLINE.SetActive(false);
			if(aa4xtextLINE) aa4xtextLINE.SetActive(true);
			if(aa8xtextLINE) aa8xtextLINE.SetActive(false);
		}

		public void AntiAliasing8X(){
			PlayerPrefs.SetInt("AntiAliasing", 8);
			QualitySettings.antiAliasing = 8;
			if(aaofftextLINE) aaofftextLINE.SetActive(false);
			if(aa2xtextLINE) aa2xtextLINE.SetActive(false);
			if(aa4xtextLINE) aa4xtextLINE.SetActive(false);
			if(aa8xtextLINE) aa8xtextLINE.SetActive(true);
		}
	}
}