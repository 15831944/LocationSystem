﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Location.Model;

namespace Location.BLL
{
    public class PositionMocker
    {
        public static List<Position> GetMockPosition(string tagCode, int count)
        {
            List<Position> positions = new List<Position>();
            for (int i = 0; i < count; i++)
            {
                Position pos = GetRandomPosition(tagCode);
                positions.Add(pos);
            }
            return positions;
        }

        public static List<Position> GetMockPosition(List<Tag> tags, int powerCount)
        {
            List<Position> positions = new List<Position>();
            for (int i = 0; i < powerCount; i++)
            {
                foreach (Tag tag in tags)
                {

                    Position pos = GetRandomPosition(tag.Code);
                    positions.Add(pos);
                }
            }
            return positions;
        }


        static Random r = new Random(DateTime.Now.Millisecond);
        public static Position GetRandomPosition(string tag)
        {
            long x = r.Next(2000);
            long y = r.Next(2000);
            long z = r.Next(200, 500);
            int number = r.Next(0, 10);
            if (string.IsNullOrEmpty(tag))
            {
                tag = string.Format("{0:0000}", number);
            }

            Position pos = new Position() { Tag = tag, Time = DateTime.Now.Ticks, X = x, Y = y, Z = z, Flag = "模拟数据" };
            return pos;
        }
    }
}
