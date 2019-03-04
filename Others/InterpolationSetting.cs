using UnityEngine;

/// <summary>
/// Provides easy helper functions for interpolating position and rotation.
/// </summary>
[System.Serializable]
public class InterpolationSetting {
	//Fields
	public bool enable = true;
	public float warpAboveDistance = 0f;
	public float interpolateHeavyAboveDistance = 0f;
	public float interpolateAboveDistance = 0f;
	public float distanceStrength = 0f;
	public float warpAboveAngle = 0f;
	public float interpolateHeavyAboveAngle = 0f;
	public float interpolateAboveAngle = 0f;
	public float angleStrength = 0f;

	InterpolationSetting savedSetting = null;


	//Functions
	public InterpolationSetting () { }

	public InterpolationSetting (InterpolationSetting pSetting) {
		InitFromSetting (pSetting);
	}

	/// <summary>
	/// Initializes this instance from an existing InterpolationSetting
	/// </summary>
	/// <param name="pSetting">The InterpolationSetting to instantiate from.</param>
	private void InitFromSetting (InterpolationSetting pSetting) {
		warpAboveDistance = pSetting.warpAboveDistance;
		interpolateHeavyAboveDistance = pSetting.interpolateHeavyAboveDistance;
		interpolateAboveDistance = pSetting.interpolateAboveDistance;
		distanceStrength = pSetting.distanceStrength;

		warpAboveAngle = pSetting.warpAboveAngle;
		interpolateHeavyAboveAngle = pSetting.interpolateHeavyAboveAngle;
		interpolateAboveAngle = pSetting.interpolateAboveAngle;
		angleStrength = pSetting.angleStrength;
	}

    /// <summary>
    /// Interpolates a Vector3 to another Vector3.
    /// </summary>
    /// <returns>The new interpolated Vector3.</returns>
    /// <param name="pFromPosition">Position to interpolate from.</param>
    /// <param name="pToPosition">Position to interpolate to.</param>
    public Vector3 InterpolatePosition (Vector3 pFromPosition, Vector3 pToPosition) {
		if (!enable) {
			return pToPosition;
		}

		//According to distance we move the client closer to the network - or not at all.
		float distance = Vector3.Distance (pFromPosition, pToPosition);
		if (distance > warpAboveDistance) {
			return pToPosition;
		} else if (distance > interpolateHeavyAboveDistance) {
			return Vector3.Lerp (pFromPosition, pToPosition, distanceStrength * 1.25f);
		} else if (distance > interpolateAboveDistance) {
			return Vector3.Lerp (pFromPosition, pToPosition, distanceStrength);
		}

		return pFromPosition;
	}

	/// <summary>
	/// Interpolates a Quaternion to another Quaternion.
	/// </summary>
	/// <returns>The new interpolated Quaternion.</returns>
	/// <param name="fromPosition">Rotation to interpolate from.</param>
	/// <param name="toPosition">Rotation to interpolate to.</param>
	public Quaternion InterpolateRotation (Quaternion pFromRotation, Quaternion pToRotation) {
		if (!enable) {
			return pToRotation;
		}

		//According to angle we rotate the client closer to the network - or not at all.
		float angleDistance = Quaternion.Angle(pFromRotation, pToRotation);
		float angleDistanceAbs = Mathf.Abs (angleDistance);
		if (angleDistanceAbs > warpAboveAngle) {
			return pToRotation;
		} else if (angleDistanceAbs > interpolateHeavyAboveAngle) {
			return Quaternion.Lerp (pFromRotation, pToRotation, angleStrength * 1.25f);
		} else if (angleDistanceAbs > interpolateAboveAngle) {
			return Quaternion.Lerp (pFromRotation, pToRotation, angleStrength);
		}

		return pFromRotation;
	}

	/// <summary>
	/// Saves the current setting to be loaded at a later time.
	/// </summary>
	public void SaveCurrentSetting () {
		savedSetting = new InterpolationSetting (this);
	}

	/// <summary>
	/// Loads the last setting that has been saved previously.
	/// </summary>
	public void LoadLastSetting () {
		if (savedSetting != null) {
			InitFromSetting (savedSetting);
			savedSetting = null;
		}
	}
}
