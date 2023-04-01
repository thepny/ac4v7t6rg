var jobselector = new Vue({
    el: ".joblist",
    data: {
        active: false,
        jobid: -1,
        level: 1,
        list: 
        [
            {class: "electro", name: "Elektriker", level: 0, jobid: 1},
            {class: "gazon", name: "Grünpfleger", level: 0, jobid: 5},
            {class: "pochta", name: "Postbote", level: 1, jobid: 2},
            {class: "taxi", name: "Taxifahrer", level: 2, jobid: 3},
            {class: "bus", name: "Busfahrer", level: 2, jobid: 4},
            {class: "mechanic", name: "Mechaniker", level: 4, jobid: 8},
            {class: "truck", name: "Trucker", level: 5, jobid: 6},
            {class: "inkos", name: "Sammler", level: 8, jobid: 7},
        ],
    },
    methods: {
        closeJobMenu: function() {
            mp.trigger("closeJobMenu");
        },
        show: function (level, currentjob) {
            this.level = level;
            this.jobid = currentjob;
            this.active = true;
        },
        hide: function () {
            this.active = false;
        },
        selectJob: function(jobid) {
            mp.trigger("selectJob", jobid);
        }
    }
})