using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Internal.Globals;

namespace Amper.NextBot;

public static class DebugOverlayExtensions
{
	public static void Arrow( this DebugOverlay debug, Vector3 startPos, Vector3 endPos, float width, Color color, float duration = 0, bool depthTest = true )
	{
		var lineDir = (endPos - startPos).Normal;
		var sideDir = lineDir.Cross( Vector3.Up );
		var radius = width * 0.5f;

		var p1 = startPos - sideDir * radius;
		var p2 = endPos - lineDir * width - sideDir * radius;
		var p3 = endPos - lineDir * width - sideDir * width;
		var p4 = endPos;
		var p5 = endPos - lineDir * width + sideDir * width;
		var p6 = endPos - lineDir * width + sideDir * radius;
		var p7 = startPos + sideDir * radius;

		// Outline the arrow
		debug.Line( p1, p2, color, duration, depthTest );
		debug.Line( p2, p3, color, duration, depthTest );
		debug.Line( p3, p4, color, duration, depthTest );
		debug.Line( p4, p5, color, duration, depthTest );
		debug.Line( p5, p6, color, duration, depthTest );
		debug.Line( p6, p7, color, duration, depthTest );
	}
}
