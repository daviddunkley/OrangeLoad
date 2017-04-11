using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using MagicApi.Models;
using MagicApi.Repositories;
using MagicApi.Services;
using Swashbuckle.Swagger.Annotations;
using Titan.Azure.Configuration;

namespace MagicApi.Controllers
{
    public class RunController : ApiController
    {
        private readonly RunRepository _runRepository;
        private readonly ImageService _imageService;

        public RunController()
        {
            var config = new AzureConfiguration();
            var accountName = config.GetAppSetting<string>("Point.AccountName");
            var accountKey = config.GetAppSetting<string>("Point.AccountKey");

            _runRepository = new RunRepository(accountName, accountKey);
            _imageService = new ImageService(accountName, accountKey);
        }

        [SwaggerOperation("GetRuns")]
        [HttpGet]
        [Route("runs")]
        public async Task<IHttpActionResult> GetRuns()
        {
            var runs = await _runRepository.GetRunsAsync();
            return Ok(runs);
        }

        [SwaggerOperation("CreateRun")]
        [HttpPost]
        [Route("runs")]
        public async Task<IHttpActionResult> CreateRun(RunRequest runReq)
        {
            var run = runReq.AsRun();

            await _runRepository.SaveRunAsync(run);

            var locationUri = new Uri(Request.RequestUri, run.Id.ToString());

            return Created(locationUri, run);
        }

        [SwaggerOperation("EndRun")]
        [HttpPost]
        [Route("runs/{runId}/end")]
        public async Task<IHttpActionResult> End(Guid runId)
        {
            var run = await _runRepository.GetRunAsync(runId);
            var imageName = _imageService.CreateImage($"{runId}.png", run.Width, run.Height);
            _runRepository.ForEachRunPoint(
                runId,
                (x, y) => _imageService.DrawPoint(imageName, x, y));

            var imageUri = _imageService.SaveImage(imageName);
            await _runRepository.EndRunAsync(runId, imageUri);

            return Created<IHttpActionResult>(imageUri, null);
        }

        [SwaggerOperation("GetRun")]
        [HttpGet]
        [Route("runs/{id}")]
        public async Task<IHttpActionResult> Get(Guid id)
        {
            var run = await _runRepository.GetRunAsync(id);
            return Ok(run);
        }

        [SwaggerOperation("CreateRunPoints")]
        [HttpPost]
        [Route("runs/{runId}/points")]
        public async Task<IHttpActionResult> CreateMany(Guid runId, RunPointRequest[] runPointReqs)
        {
            var run = await _runRepository.GetRunAsync(runId);

            await Task.WhenAll(runPointReqs.Select(runPointReq => SaveRunPoint(run, runPointReq.AsRunPoint(run.Id))).Cast<Task>().ToArray());

            var locationUri = new Uri($"{Request.RequestUri}/{runPointReqs.LastOrDefault()?.Id}");

            return Created(locationUri, default(RunPoint));
        }

        [SwaggerOperation("CreateRunPoint")]
        [HttpPost]
        [Route("runs/{runId}/point")]
        public async Task<IHttpActionResult> CreateAsync(Guid runId, RunPointRequest runPointReq)
        {
            var run = await _runRepository.GetRunAsync(runId);

            var runPoint = await SaveRunPoint(run, runPointReq);

            var locationUri = new Uri($"{Request.RequestUri}/{runPoint.Id}");

            return Created(locationUri, runPoint);
        }

        private async Task<RunPoint> SaveRunPoint(Run run, RunPointRequest runPointReq)
        {
            if (runPointReq.X >= run.Width ||
                runPointReq.X < 0 ||
                runPointReq.Y >= run.Height ||
                runPointReq.Y < 0)
            {
                throw new Exception($"Point ({runPointReq.X},{runPointReq.Y}) is outside the bounds of the Run dimensions ({run.Width},{run.Height}.");
            }

            var runPoint = runPointReq.AsRunPoint(run.Id);

            await _runRepository.SavePointAsync(runPoint);

            return runPoint;
        }

        [SwaggerOperation("GetRunPoint")]
        [HttpGet]
        [Route("runs/{runId}/points/{id}")]
        public async Task<IHttpActionResult> GetPoint(Guid runId, Guid id)
        {
            var runPoint = await _runRepository.GetRunPointAsync(runId, id);
            return Ok(runPoint);
        }
    }
}
