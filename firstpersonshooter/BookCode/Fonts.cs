#region Using Statements
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Text;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
#endregion

namespace BookCode
{
    static class Fonts
    {
        #region Fonts

        private static SpriteFont headerFont;
        public static SpriteFont HeaderFont
        {
            get { return headerFont; }
        }

        private static SpriteFont menuFont;
        public static SpriteFont MenuFont
        {
            get { return menuFont; }
        }

        private static SpriteFont highlightFont;
        public static SpriteFont HighlightFont
        {
            get { return menuFont; }
        }

        #endregion


        #region Font Colors

        public static readonly Color HeaderColor = Color.OliveDrab;
        public static readonly Color MenuColor = Color.OliveDrab;
        public static readonly Color HighlightColor = Color.Sienna;
        public static readonly Color PlayerColor = Color.Red;
        public static readonly Color AboutColor = Color.Yellow;
        #endregion


        #region Initialization

        public static void LoadContent(ContentManager contentManager)
        {
            if (contentManager == null)
            {
                throw new ArgumentNullException("contentManager");
            }
            headerFont = contentManager.Load<SpriteFont>("stencil");
            menuFont = contentManager.Load<SpriteFont>("stencil");
            highlightFont = contentManager.Load<SpriteFont>("stencil");
        }

        #endregion

    }
}
