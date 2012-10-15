First-Person-Shooter-Demo
=========================

A first person shooter demo using XNA 3.0

Information
-----------------------

This project was created as a final for CIS 490 at K-State University. It was made with an old version of XNA. I don't know if it works with the new version, so the code is offered as is.  

Old Readme
=============================

Here is my old readme from 2010 when I made this project.

Readme
----------------

Depending on what graphics card you grade this on, the heightmap might actually be too big for the card to render the entire thing, thus not showing up. If this happens when you open my project follow this guideline.

Terrain.cs in the constructor

This is the default, it is pretty large and doesn't load on my laptop. 
	
* Texture2D heightMap = content.Load<Texture2D>("Heightmap");

This is a bit smaller than my big one, but works okay on my laptop with a horrible graphics card. Use this if the default doesn't work.

* Texture2D heightMap = content.Load<Texture2D>("laptopHeightMap");   

This is the smallest of the three, use this if NEITHER of them work, but I don't see that happening unless you are using a computer over 3 years old to grade this.
         
* Texture2D heightMap = content.Load<Texture2D>("HeightmapLG"); 

Thanks!