// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;
using UnityEngine;

namespace EchKode.PBMods.Fixes
{
    static class CombatUtilities
    {
		public static string GetHitDirection(Quaternion objectRotation, Vector3 incomingDirection)
		{
			// incomingDirection is expected to be a normalized vector.
			//
			// This routine checks the collinearity of the incoming direction with the local transform's
			// X and Z axes using the Dot() function. Its return value is the cosine of the angle between
			// the incoming direction and the respective axis. This is only true if incomingDirection is
			// normalized. Keep in mind that the cosine of angles between 90 and 270 is negative (180 = -1).
			//
			// The logic below describes 4 polar zones: 2 120-degree zones for front and back; and 2 60-degree
			// zones for each side (0.5 is the cosine of an angle of 30 degrees).

			var incoming2D = Utilities.Flatten(incomingDirection);
			var frontNormal = Utilities.Flatten(objectRotation * Vector3.forward);
			var sideNormal = Utilities.Flatten(objectRotation * Vector3.right);
			var frontDot = Vector3.Dot(frontNormal, incoming2D);
			var sideDot = Vector3.Dot(sideNormal, incoming2D);
			return frontDot < 0.0f
				? sideDot < 0.0f
					? frontDot >= -0.5f
						? HitDirections.left
						: HitDirections.back  // original code: "right"
					: frontDot >= -0.5f
						? HitDirections.right
						: HitDirections.back
				: sideDot < 0.0f
					? frontDot >= 0.5f
						? HitDirections.front
						: HitDirections.left
					: frontDot >= 0.5f
						? HitDirections.front
						: HitDirections.right;
		}
	}
}
