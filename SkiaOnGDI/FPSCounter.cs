using System;

namespace SkiaOnGDI
{

    class FPSCounter
    {
        int fpsStep = 0;
        int lastFps = 0;
        DateTime lastDate = DateTime.Now;

        public int FPS
        {
            get
            {
                return lastFps;
            }
        }

        public void Update()
        {
            fpsStep++;

            var delta = DateTime.Now - lastDate;

            if (delta.TotalSeconds >= 1)
            {
                lastDate = DateTime.Now;

                lastFps = fpsStep;
                fpsStep = 0;
            }
        }
    }
}
