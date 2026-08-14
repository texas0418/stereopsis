using UnityEngine;

namespace Stereopsis
{
    public static class PickUtil
    {
        /// <summary>
        /// Finds the nearest component T along a click ray, forgiving
        /// grazes: T still wins if its hit lies no more than
        /// <paramref name="tolerance"/> metres behind the first solid hit
        /// — so a table edge cannot eat a click aimed at the small box
        /// sitting on it — but clicks never reach through real walls.
        /// </summary>
        public static T Pick<T>(Camera cam, Vector2 screenPos, LayerMask mask, float tolerance)
            where T : Component
        {
            var ray = cam.ScreenPointToRay(screenPos);
            var hits = Physics.RaycastAll(ray, 100f, mask);
            if (hits.Length == 0) return null;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            float firstSolid = hits[0].distance;
            foreach (var h in hits)
            {
                if (h.distance > firstSolid + tolerance) break;
                var c = h.collider.GetComponentInParent<T>();
                if (c != null) return c;
            }
            return null;
        }
    }
}
