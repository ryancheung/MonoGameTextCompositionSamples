using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using SpriteFontPlus;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using MonoGame.Framework.Utilities;

namespace Sample
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        DynamicSpriteFont font1;

        KeyboardState lastState;
        Texture2D whitePixel;
        string inputContent = string.Empty;

        private string _CompositionString = "|";

        private IMEString[] _CandidateList;
        private int _CandidateSelection;

        static Game1()
        {
            // Show OS IME Window
            //ImmService.ShowOSImeWindow = true;
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            IsMouseVisible = true;

            Window.ImmService.TextInput += (o, e) =>
            {
                switch ((int)e.Character)
                {
                    case 8:
                        if (inputContent.Length > 0)
                        {
                            int lengthToRemove = 1;

                            char char1 = inputContent[inputContent.Length - 1];
                            if (char.IsSurrogate(char1))
                            {
                                char char2 = inputContent[inputContent.Length - 1 - 1];
                                if (char.IsSurrogatePair(char2, char1))
                                    lengthToRemove = 2;
                            }
                            inputContent = inputContent.Remove(inputContent.Length - lengthToRemove, lengthToRemove);
                        }
                        break;
                    case 27:
                    case 13:
                        inputContent = "";
                        break;
                    default:
                        if (char.IsControl(e.Character))
                            break;

                        inputContent += e.Character;
                        break;
                }

                inputContent = inputContent.Trim();
            };

            Window.ImmService.TextComposition += (o, e) =>
            {
                _CompositionString = e.CompositionText.ToString();
                _CompositionString = _CompositionString.Insert(e.CursorPosition, "|");

                _CandidateList = e.CandidateList;
                _CandidateSelection = e.CandidateSelection;

                Vector2 textSize = font1.MeasureString(inputContent + _CompositionString);

                var rect = new Rectangle(10 + (int)textSize.X, 50 + 32, 0, 40);
                Window.ImmService.SetTextInputRect(rect);
            };

            var rect2 = new Rectangle(10, 50 + 32, 0, 0);
            Window.ImmService.SetTextInputRect(rect2);

            base.Initialize();
        }

        public static byte[] GetManifestResourceStream(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resNames = assembly.GetManifestResourceNames();

            var actualResourceName = resNames.First(r => r.EndsWith(resourceName));

            var stream = assembly.GetManifestResourceStream(actualResourceName);
            byte[] ret = new byte[stream.Length];
            stream.Read(ret, 0, (int)stream.Length);

            return ret;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            font1 = DynamicSpriteFont.FromTtf(GetManifestResourceStream("SourceHanSans-Normal.otf"), 30);
            font1.AddTtf(GetManifestResourceStream("seguiemj.ttf"));

            whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            whitePixel.SetData<Color>(new Color[] { Color.White });

            Window.ImmService.StartTextInput();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (PlatformInfo.MonoGamePlatform == MonoGamePlatform.iOS || PlatformInfo.MonoGamePlatform == MonoGamePlatform.Android)
            {
            }
            else
            {
                KeyboardState ks = Keyboard.GetState();

                if (ks.IsKeyDown(Keys.F1) && lastState.IsKeyUp(Keys.F1))
                {
                    if (Window.ImmService.IsTextInputActive)
                        Window.ImmService.StopTextInput();
                    else
                        Window.ImmService.StartTextInput();
                }

                lastState = ks;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();

            Vector2 textSize = font1.MeasureString(inputContent);

            if (PlatformInfo.MonoGamePlatform == MonoGamePlatform.iOS || PlatformInfo.MonoGamePlatform == MonoGamePlatform.Android)
                spriteBatch.DrawString(font1, "Touch screen to Enable/Disable Text Composition", new Vector2(10, 10), Color.White);
            else
                spriteBatch.DrawString(font1, "Press F1 to Enable/Disable Text Composition", new Vector2(10, 10), Color.White);

            if (Window.ImmService.IsTextInputActive)
                spriteBatch.DrawString(font1, "Text Composition enabled.", new Vector2(10, 40), Color.LightGray);
            else
                spriteBatch.DrawString(font1, "Text Composition disabled.", new Vector2(10, 40), Color.LightGray);

            int offsetX = 10;
            int offsetY = 40 + 50;

            // Draw text inputs
            spriteBatch.DrawString(font1, inputContent, new Vector2(offsetX, offsetY), Color.White);

            // Draw text composition string
            var compStrDrawX = offsetX + (int)textSize.X + 2;
            spriteBatch.DrawString(font1, _CompositionString, new Vector2(compStrDrawX, offsetY), Color.Orange);

            int lineHeight = 32;

            offsetY += 32;

            // Draw candidate list
            if (_CandidateList != null)
            {
                for (int i = 0; i < _CandidateList.Length; i++)
                {
                    try
                    {
                        var candidateStr = string.Format("{0}.{1}", i + 1, _CandidateList[i]);
                        var candidateDrawPos = new Vector2(offsetX + textSize.X, offsetY + i * lineHeight);

                        spriteBatch.DrawString(font1, candidateStr, candidateDrawPos, i == _CandidateSelection ? Color.Yellow : Color.White);
                    }
                    catch
                    {

                    }
                }
            }

            if (PlatformInfo.MonoGamePlatform == MonoGamePlatform.iOS || PlatformInfo.MonoGamePlatform == MonoGamePlatform.Android)
            {
                spriteBatch.DrawString(font1, $"Virtual keyboard heght: {Window.ImmService.VirtualKeyboardHeight}", new Vector2(offsetX, 150), Color.Orange);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}