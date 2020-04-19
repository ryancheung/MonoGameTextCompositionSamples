using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpriteFontPlus;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Sample.WindowsDX
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

        const int UnicodeSimplifiedChineseMin = 0x4E00;
        const int UnicodeSimplifiedChineseMax = 0x9FA5;
        const string DefaultChar = "□";

        private string _CompositionString = string.Empty;
        private int _CursorPosition = -1;

        private string[] _CandidateList = new string[] { };
        private uint _CandidatePageStart;
        private uint _CandidatePageSize;
        private uint _CandidateSelection;

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
                            inputContent = inputContent.Remove(inputContent.Length - 1, 1);
                        break;
                    case 27:
                    case 13:
                        inputContent = "";
                        break;
                    default:
                        if (char.IsSurrogate(e.Character))
                            break;
                        if (e.Character > UnicodeSimplifiedChineseMax)
                            inputContent += DefaultChar;
                        else
                            inputContent += e.Character;
                        break;
                }

                inputContent = inputContent.Trim();
            };

            Window.ImmService.TextComposition += (o, e) =>
            {
                _CompositionString = e.CompositionString;
                _CursorPosition = e.CursorPosition;

                _CandidateList = e.CandidateList;
                _CandidatePageStart = e.CandidatePageStart;
                _CandidatePageSize = e.CandidatePageSize;
                _CandidateSelection = e.CandidateSelection;

                var rect = new Rectangle(10, 50, 0, 0);
                Window.ImmService.SetTextInputRect(rect);
            };

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

            whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            whitePixel.SetData<Color>(new Color[] { Color.White });
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

            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.F1) && lastState.IsKeyUp(Keys.F1))
            {
                if (Window.ImmService.IsTextInputActive)
                    Window.ImmService.StopTextInput();
                else
                    Window.ImmService.StartTextInput();
            }

            lastState = ks;

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

            spriteBatch.DrawString(font1, "Press F1 to Enable/Disable Text Composition", new Vector2(10, 10), Color.White);

            int offsetX = 10;
            int offsetY = 50;

            // Draw text inputs
            spriteBatch.DrawString(font1, inputContent, new Vector2(offsetX, offsetY), Color.White);

            // Draw text composition string
            var compStrDrawX = offsetX + (int)textSize.X + 2;
            spriteBatch.DrawString(font1, _CompositionString, new Vector2(compStrDrawX, offsetY), Color.Orange);

            // Draw cursor
            Vector2 cursorDrawPos = new Vector2(compStrDrawX, offsetY);
            Color compColor = Color.White;

            if (_CursorPosition >= 0)
                spriteBatch.Draw(whitePixel, new Rectangle((int)cursorDrawPos.X, (int)cursorDrawPos.Y, 1, (int)font1.Size), Color.White);

            int lineHeight = 32;

            offsetY += 32;

            // Draw candidate list
            if (_CandidateList != null)
            {
                for (uint i = _CandidatePageStart; i < Math.Min(_CandidatePageStart + _CandidatePageSize, _CandidateList.Length); i++)
                {
                    for (int j = 0; j < _CandidateList[i].Length; j++)
                    {
                        if (_CandidateList[i][0] > UnicodeSimplifiedChineseMax)
                            _CandidateList[i] = DefaultChar;
                    }

                    try
                    {
                        var candidateStr = string.Format("{0}.{1}", i + 1 - _CandidatePageStart, _CandidateList[i]);
                        var candidateDrawPos = new Vector2(offsetX + textSize.X, offsetY + (i - _CandidatePageStart) * lineHeight);

                        spriteBatch.DrawString(font1, candidateStr, candidateDrawPos, i == _CandidateSelection ? Color.Yellow : Color.White);
                    }
                    catch
                    {

                    }
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
