using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace RegionFlags
{
    class PositionQueue
    {
        private Queue<Vector2> positions;
        public PositionQueue()
        {
            positions = new Queue<Vector2>();
        }

        public void enqueue( Vector2 pos )
        {
            positions.Enqueue(pos);
            if (positions.Count > 3)
                positions.Dequeue();
        }

        public void reset( Vector2 pos )
        {
            positions.Clear();
            positions.Enqueue(pos);
        }

    }
}
