namespace L4d2AddonsMgr.MeowTaskSpace {

    public interface IMeowTask {

        void Do();

        bool RequestCancel();
    }
}
