namespace rk45
{
    public abstract class OdeBase
    {
        // Define A(i)
        protected const double
            A1 = 0.0, A2 = 1.0 / 4, A3 = 3.0 / 8, A4 = 12.0 / 13, A5 = 1.0, A6 = 1.0 / 2;

        // Define B(i, j)
        protected const double
            B21 = 1.0 / 4,
            B31 = 3.0 / 32, B32 = 9.0 / 32,
            B41 = 1932.0 / 2197, B42 = -7200.0 / 2197, B43 = 7296.9 / 2197,
            B51 = 439.0 / 216, B52 = -8.0, B53 = 3680.0 / 513, B54 = -845.0 / 4104,
            B61 = -8.0 / 27, B62 = 2.0, B63 = -3544.0 / 2565, B64 = 1859.0 / 4104, B65 = -11.0 / 40;

        // Define Ch(i)
        protected const double
            Ch1 = 16.0 / 135, Ch2 = 0.0, Ch3 = 6656.0 / 12825, Ch4 = 28561.0 / 56430, Ch5 = -9.0 / 50, Ch6 = 2.0 / 55;

        // Define Ct(i)
        protected const double
            Ct1 = 1.0 / 360, Ct2 = 0.0, Ct3 = -128.0 / 4275, Ct4 = -2187.0 / 75240, Ct5 = 1.0 / 50, Ct6 = 2.0 / 55;
    }
}
